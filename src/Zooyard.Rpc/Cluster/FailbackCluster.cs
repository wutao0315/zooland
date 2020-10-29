using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Diagnositcs;
using Zooyard.Core.Logging;

namespace Zooyard.Rpc.Cluster
{
    /// <summary>
    /// 失败自动恢复，后台记录失败请求，定时重发，通常用于消息通知操作。
    /// </summary>
    public class FailbackCluster : AbstractCluster
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(FailbackCluster));
        public override string Name => NAME;
        public const string NAME = "failback";
        private static readonly long RETRY_FAILED_PERIOD = 5 * 1000;

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
                        retryTimer.Elapsed += new System.Timers.ElapsedEventHandler(async (object sender, System.Timers.ElapsedEventArgs events) =>
                        {
                            // 收集统计信息
                            try
                            {
                                await retryFailed(pool).ConfigureAwait(false);
                            }
                            catch (Exception t)
                            { // 防御性容错
                                Logger().Error(t, $"Unexpected error occur at collect statistic {t.Message}");
                            }
                        });
                        retryTimer.AutoReset = true;
                        retryTimer.Enabled = true;
                    }
                }
            }

            failed.TryAdd(invocation, router);
        }



        async Task retryFailed(IClientPool pool)
        {
            if (failed.Count == 0)
            {
                return;
            }
            foreach (var entry in failed)
            {
                IInvocation invocation = entry.Key;
                var client = await pool.GetClient(entry.Value);
                try
                {
                    var refer = await client.Refer();
                    _source.WriteConsumerBefore(refer.Instance, entry.Value, invocation);
                    var result = await refer.Invoke(invocation);
                    _source.WriteConsumerAfter(entry.Value, invocation, result);
                    pool.Recovery(client);
                    URL cluster;
                    failed.TryRemove(invocation ,out cluster);
                }
                catch (Exception e)
                {
                    _source.WriteConsumerError(entry.Value, invocation, e);
                    await pool.DestoryClient(client).ConfigureAwait(false);
                    Logger().Error(e, $"Failed retry to invoke method {invocation.MethodInfo.Name}, waiting again.");
                }
            }
        }

        public override async Task<IClusterResult> DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            Exception exception = null;

            checkInvokers(urls, invocation, address);
            var invoker = base.select(loadbalance, invocation, urls, null);

            IResult result;
            try
            {
                var client = await pool.GetClient(invoker);
                try
                {
                    var refer = await client.Refer();
                    _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                    result = await refer.Invoke(invocation);
                    _source.WriteConsumerAfter(invoker, invocation, result);
                    pool.Recovery(client);
                    goodUrls.Add(invoker);
                }
                catch (Exception ex)
                {
                    await pool.DestoryClient(client).ConfigureAwait(false);
                    _source.WriteConsumerError(invoker, invocation, ex);
                    throw ex;
                }
            }
            catch (Exception e)
            {
                Logger().Error(e, $"Failback to invoke method {invocation.MethodInfo.Name}, wait for retry in background. Ignored exception:{e.Message}");
                addFailed(pool, invocation, invoker);
                result = new RpcResult(); // ignore
                exception = e;
                badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
            }

            return new ClusterResult(result, goodUrls, badUrls, exception, false);
        }
    }
}
