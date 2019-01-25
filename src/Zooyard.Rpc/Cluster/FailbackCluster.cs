using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooyard.Core;
using Zooyard.Core.Diagnositcs;

namespace Zooyard.Rpc.Cluster
{
    /// <summary>
    /// 失败自动恢复，后台记录失败请求，定时重发，通常用于消息通知操作。
    /// </summary>
    public class FailbackCluster : AbstractCluster
    {
        public override string Name => NAME;
        public const string NAME = "failback";
        private static readonly long RETRY_FAILED_PERIOD = 5 * 1000;

        private readonly ILogger _logger;
        public FailbackCluster(ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FailbackCluster>();
        }
        private ConcurrentDictionary<IInvocation, URL> failed = new ConcurrentDictionary<IInvocation, URL>();
        private System.Timers.Timer retryTimer;


        private void addFailed(IClientPool pool,IInvocation invocation, URL router)
        {
           
            if (retryTimer == null)
            {
                lock (this)
                {
                    if (retryTimer == null)
                    {
                        retryTimer = new System.Timers.Timer(RETRY_FAILED_PERIOD);
                        retryTimer.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs events) =>
                        {
                            // 收集统计信息
                            try
                            {
                                retryFailed(pool);
                            }
                            catch (Exception t)
                            { // 防御性容错
                                _logger.LogError(t,"Unexpected error occur at collect statistic");
                            }
                        });
                        retryTimer.AutoReset = true;
                        retryTimer.Enabled = true;
                    }
                }
            }

            failed.TryAdd(invocation, router);
        }



        void retryFailed(IClientPool pool)
        {
            if (failed.Count == 0)
            {
                return;
            }
            foreach (var entry in failed)
            {
                IInvocation invocation = entry.Key;
                var client = pool.GetClient(entry.Value);
                try
                {
                    var refer = client.Refer();
                    _source.WriteConsumerBefore(invocation);
                    var result = refer.Invoke(invocation);
                    _source.WriteConsumerAfter(invocation, result);
                    pool.Recovery(client);
                    URL cluster;
                    failed.TryRemove(invocation ,out cluster);
                }
                catch (Exception e)
                {
                    pool.Recovery(client);
                    _logger.LogError(e, $"Failed retry to invoke method {invocation.MethodInfo.Name}, waiting again.");
                }
            }
        }

        public override IClusterResult DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            IResult result = null;
            Exception exception = null;

            checkInvokers(urls, invocation, address);
            var invoker = base.select(loadbalance, invocation, urls, null);

            try
            {
                var client = pool.GetClient(invoker);
                try
                {
                    var refer = client.Refer();
                    _source.WriteConsumerBefore(invocation);
                    result = refer.Invoke(invocation);
                    _source.WriteConsumerAfter(invocation, result);
                    pool.Recovery(client);
                    goodUrls.Add(invoker);
                }
                catch (Exception ex)
                {

                    pool.Recovery(client);
                    throw ex;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failback to invoke method {invocation.MethodInfo.Name}, wait for retry in background. Ignored exception:{e.Message}");
                addFailed(pool,invocation, invoker);
                result = new RpcResult(); // ignore
                exception = e;
                badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
            }

            return new ClusterResult(result, goodUrls, badUrls, exception, false);
        }
    }
}
