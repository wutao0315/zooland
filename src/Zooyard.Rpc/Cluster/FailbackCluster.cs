using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Cluster
{
    /// <summary>
    /// 失败自动恢复，后台记录失败请求，定时重发，通常用于消息通知操作。
    /// </summary>
    public class FailbackCluster : AbstractCluster
    {
        public const string NAME = "failback";
        private static readonly long RETRY_FAILED_PERIOD = 5 * 1000;

        //private final ScheduledExecutorService scheduledExecutorService = Executors.newScheduledThreadPool(2, new NamedThreadFactory("failback-cluster-timer", true));
        private ConcurrentDictionary<IInvocation, URL> failed = new ConcurrentDictionary<IInvocation, URL>();
        //private volatile ScheduledFuture retryFuture;
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
                                //Thread.Sleep(TimeSpan.FromMilliseconds(RETRY_FAILED_PERIOD));
                                retryFailed(pool);
                            }
                            catch (Exception t)
                            { // 防御性容错
                                //logger.Error("Unexpected error occur at collect statistic", t);
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
                    var result = refer.Invoke(invocation);
                    pool.Recovery(client);
                    URL cluster;
                    failed.TryRemove(invocation ,out cluster);
                }
                catch (Exception e)
                {
                    pool.Recovery(client);
                    //logger.Error("Failed retry to invoke method " + invocation.MethodInfo.Name + ", waiting again.", e);
                }
            }
        }

        public override IClusterResult DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            IResult result = null;
            Exception exception = null;

            checkInvokers(urls, invocation);
            var invoker = base.select(loadbalance, invocation, urls, null);

            try
            {
                var client = pool.GetClient(invoker);
                try
                {
                    var refer = client.Refer();
                    result = refer.Invoke(invocation);
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
                
                //logger.Error("Failback to invoke method " + invocation.MethodInfo.Name + ", wait for retry in background. Ignored exception: " + e.Message + ", ", e);
                addFailed(pool,invocation, invoker);
                result = new RpcResult(); // ignore
                exception = e;
                badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
            }

            return new ClusterResult(result, goodUrls, badUrls, exception, false);
        }
    }
}
