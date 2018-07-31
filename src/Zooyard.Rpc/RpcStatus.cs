using System;
using System.Collections.Concurrent;
using System.Threading;
using Zooyard.Core;
using Zooyard.Core.Atomic;

namespace Zooyard.Rpc
{
    public class RpcStatus
    {
        private static ConcurrentDictionary<string, RpcStatus> SERVICE_STATISTICS = new ConcurrentDictionary<String, RpcStatus>();

        private static ConcurrentDictionary<string, ConcurrentDictionary<String, RpcStatus>> METHOD_STATISTICS = new ConcurrentDictionary<string, ConcurrentDictionary<String, RpcStatus>>();
        private ConcurrentDictionary<string, object> values = new ConcurrentDictionary<string, object>();
        private AtomicInteger active = new AtomicInteger();
        private AtomicLong total = new AtomicLong();
        private AtomicInteger failed = new AtomicInteger();
        private AtomicLong totalElapsed = new AtomicLong();
        private AtomicLong failedElapsed = new AtomicLong();
        private AtomicLong maxElapsed = new AtomicLong();
        private AtomicLong failedMaxElapsed = new AtomicLong();
        private AtomicLong succeededMaxElapsed = new AtomicLong();
        
        /**
         * 用来实现executes属性的并发限制（即控制能使用的线程数）
         * 2017-08-21 yizhenqiang
         */
        private volatile Semaphore executesLimit;
        private volatile int executesPermits;

        private RpcStatus()
        {
        }

        /**
         * @param url
         * @return status
         */
        public static RpcStatus GetStatus(URL url)
        {
            var uri = url.ToIdentityString();
            var status = SERVICE_STATISTICS[uri];
            if (status == null)
            {
                SERVICE_STATISTICS.GetOrAdd(uri, new RpcStatus());
                status = SERVICE_STATISTICS[uri];
            }
            return status;
        }

        /**
         * @param url
         */
        public static void RemoveStatus(URL url)
        {
            var uri = url.ToIdentityString();
            RpcStatus rpcStatus = null;
            SERVICE_STATISTICS.TryRemove(uri,out rpcStatus);
        }

        /**
         * @param url
         * @param methodName
         * @return status
         */
        public static RpcStatus GetStatus(URL url, String methodName)
        {
            var uri = url.ToIdentityString();

            if (!METHOD_STATISTICS.ContainsKey(uri))
            {
                METHOD_STATISTICS.GetOrAdd(uri, new ConcurrentDictionary<String, RpcStatus>());
                //map = METHOD_STATISTICS[uri];
            }
            var map = METHOD_STATISTICS[uri];
            
            
            if (!map.ContainsKey(methodName))
            {
                map.GetOrAdd(methodName, new RpcStatus());
                //status = map[methodName];
            }
            var status = map[methodName];
            return status;
        }

        /**
         * @param url
         */
        public static void RemoveStatus(URL url, String methodName)
        {
            var uri = url.ToIdentityString();
            if (!METHOD_STATISTICS.ContainsKey(uri))
            {
                var map = METHOD_STATISTICS[uri];
                RpcStatus rpcStatus = null;
                map.TryRemove(methodName, out rpcStatus);
            }
            
            //if (map != null)
            //{

            //}
        }

        /**
         * @param url
         */
        public static void BeginCount(URL url, String methodName)
        {
            beginCount(GetStatus(url));
            beginCount(GetStatus(url, methodName));
        }

        private static void beginCount(RpcStatus status)
        {
            status.active.IncrementAndGet();
        }

        /**
         * @param url
         * @param elapsed
         * @param succeeded
         */
        public static void EndCount(URL url, String methodName, long elapsed, bool succeeded)
        {
            endCount(GetStatus(url), elapsed, succeeded);
            endCount(GetStatus(url, methodName), elapsed, succeeded);
        }

        private static void endCount(RpcStatus status, long elapsed, bool succeeded)
        {
            status.active.DecrementAndGet();
            status.total.IncrementAndGet();
            status.totalElapsed.AddAndGet(elapsed);
            if (status.maxElapsed.Value < elapsed)
            {
                status.maxElapsed.Value= elapsed;
            }
            if (succeeded)
            {
                if (status.succeededMaxElapsed.Value < elapsed)
                {
                    status.succeededMaxElapsed.Value=elapsed;
                }
            }
            else
            {
                status.failed.IncrementAndGet();
                status.failedElapsed.AddAndGet(elapsed);
                if (status.failedMaxElapsed.Value < elapsed)
                {
                    status.failedMaxElapsed.Value=elapsed;
                }
            }
        }

        /**
         * set value.
         *
         * @param key
         * @param value
         */
        public void Set(string key, object value)
        {
            values[key]= value;
        }

        /**
         * get value.
         *
         * @param key
         * @return value
         */
        public object Get(string key)
        {
            return values[key];
        }

        /**
         * get active.
         *
         * @return active
         */
        public int GetActive()
        {
            return active.Value;
        }

        /**
         * get total.
         *
         * @return total
         */
        public long GetTotal()
        {
            return total.Value;
        }

        /**
         * get total elapsed.
         *
         * @return total elapsed
         */
        public long GetTotalElapsed()
        {
            return totalElapsed.Value;
        }

        /**
         * get average elapsed.
         *
         * @return average elapsed
         */
        public long GetAverageElapsed()
        {
            long total = GetTotal();
            if (total == 0)
            {
                return 0;
            }
            return GetTotalElapsed() / total;
        }

        /**
         * get max elapsed.
         *
         * @return max elapsed
         */
        public long GetMaxElapsed()
        {
            return maxElapsed.Value;
        }

        /**
         * get failed.
         *
         * @return failed
         */
        public int GetFailed()
        {
            return failed.Value;
        }

        /**
         * get failed elapsed.
         *
         * @return failed elapsed
         */
        public long GetFailedElapsed()
        {
            return failedElapsed.Value;
        }

        /**
         * get failed average elapsed.
         *
         * @return failed average elapsed
         */
        public long GetFailedAverageElapsed()
        {
            long failed = GetFailed();
            if (failed == 0)
            {
                return 0;
            }
            return GetFailedElapsed() / failed;
        }

        /**
         * get failed max elapsed.
         *
         * @return failed max elapsed
         */
        public long GetFailedMaxElapsed()
        {
            return failedMaxElapsed.Value;
        }

        /**
         * get succeeded.
         *
         * @return succeeded
         */
        public long GetSucceeded()
        {
            return GetTotal() - GetFailed();
        }

        /**
         * get succeeded elapsed.
         *
         * @return succeeded elapsed
         */
        public long GetSucceededElapsed()
        {
            return GetTotalElapsed() - GetFailedElapsed();
        }

        /**
         * get succeeded average elapsed.
         *
         * @return succeeded average elapsed
         */
        public long GetSucceededAverageElapsed()
        {
            long succeeded = GetSucceeded();
            if (succeeded == 0)
            {
                return 0;
            }
            return GetSucceededElapsed() / succeeded;
        }

        /**
         * get succeeded max elapsed.
         *
         * @return succeeded max elapsed.
         */
        public long GetSucceededMaxElapsed()
        {
            return succeededMaxElapsed.Value;
        }

        /**
         * Calculate average TPS (Transaction per second).
         *
         * @return tps
         */
        public long GetAverageTps()
        {
            if (GetTotalElapsed() >= 1000L)
            {
                return GetTotal() / (GetTotalElapsed() / 1000L);
            }
            return GetTotal();
        }

        /**
         * 获取限制线程数的信号量，信号量的许可数就是executes设置的值
         * 2017-08-21 yizhenqiang
         * @param maxThreadNum executes设置的值
         * @return
         */
        public Semaphore GetSemaphore(int maxThreadNum)
        {
            if (maxThreadNum <= 0)
            {
                return null;
            }

            if (executesLimit == null || executesPermits != maxThreadNum)
            {
                lock(this) {
                    if (executesLimit == null || executesPermits != maxThreadNum)
                    {
                        executesLimit = new Semaphore(1,maxThreadNum);
                        executesPermits = maxThreadNum;
                    }
                }
            }

            return executesLimit;
        }
    }
}
