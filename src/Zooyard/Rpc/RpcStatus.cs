﻿using System.Collections.Concurrent;
using Zooyard.Atomic;

namespace Zooyard.Rpc;

public class RpcStatus
{
    private static readonly ConcurrentDictionary<string, RpcStatus> SERVICE_STATUS_MAP = new();
    private readonly static ConcurrentDictionary<string, RpcStatus> SERVICE_STATISTICS = new();
    private readonly static ConcurrentDictionary<string, ConcurrentDictionary<string, RpcStatus>> METHOD_STATISTICS = new();

    private readonly ConcurrentDictionary<string, object> values = new();
    private readonly AtomicInteger active = new();
    private readonly AtomicLong total = new();
    private readonly AtomicInteger failed = new();
    private readonly AtomicLong totalElapsed = new();
    private readonly AtomicLong failedElapsed = new();
    private readonly AtomicLong maxElapsed = new();
    private readonly AtomicLong failedMaxElapsed = new();
    private readonly AtomicLong succeededMaxElapsed = new();

    /// <summary>
    /// 用来实现executes属性的并发限制（即控制能使用的线程数）
    /// </summary>
    private volatile Semaphore? executesLimit;
    private volatile int executesPermits;

    private RpcStatus() { }

    /// <summary>
    /// get status
    /// </summary>
    /// <param name="url">url</param>
    /// <returns>status</returns>
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

    /// <summary>
    /// remove status
    /// </summary>
    /// <param name="url">url</param>
    public static void RemoveStatus(URL url)
    {
        var uri = url.ToIdentityString();
        SERVICE_STATISTICS.TryRemove(uri,out _);
    }

    /// <summary>
    /// get status
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="methodName">method name</param>
    /// <returns></returns>
    public static RpcStatus GetStatus(URL url, string methodName)
    {
        var uri = url.ToIdentityString();

        if (!METHOD_STATISTICS.ContainsKey(uri))
        {
            METHOD_STATISTICS.GetOrAdd(uri, new ConcurrentDictionary<string, RpcStatus>());
        }
        var map = METHOD_STATISTICS[uri];
        
        
        if (!map.ContainsKey(methodName))
        {
            map.GetOrAdd(methodName, new RpcStatus());
        }
        var status = map[methodName];
        return status;
    }

    /// <summary>
    /// remove status
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="methodName">method Name</param>
    public static void RemoveStatus(URL url, string methodName)
    {
        var uri = url.ToIdentityString();
        if (METHOD_STATISTICS.TryGetValue(uri, out var map))
        {
            map.TryRemove(methodName, out _);
        }
    }

    /// <summary>
    /// begin count
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="methodName">method Name</param>
    public static void BeginCount(URL url, string methodName)
    {
        BeginCount(GetStatus(url));
        BeginCount(GetStatus(url, methodName));
    }

    private static void BeginCount(RpcStatus status)
    {
        status.active.IncrementAndGet();
    }

   /// <summary>
   /// end count
   /// </summary>
   /// <param name="url"></param>
   /// <param name="methodName"></param>
   /// <param name="elapsed"></param>
   /// <param name="succeeded"></param>
    public static void EndCount(URL url, string methodName, long elapsed, bool succeeded)
    {
        EndCount(GetStatus(url), elapsed, succeeded);
        EndCount(GetStatus(url, methodName), elapsed, succeeded);
    }

    private static void EndCount(RpcStatus status, long elapsed, bool succeeded)
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

    /// <summary>
		/// get the RpcStatus of this service
		/// </summary>
		/// <param name="service"> the service </param>
		/// <returns> RpcStatus </returns>
		public static RpcStatus GetStatus(string service)
    {
        return SERVICE_STATUS_MAP.GetOrAdd(service, key => new RpcStatus());
    }

    /// <summary>
    /// remove the RpcStatus of this service
    /// </summary>
    /// <param name="service"> the service </param>
    public static void RemoveStatus(string service)
    {
        SERVICE_STATUS_MAP.TryRemove(service, out _);
    }

    /// <summary>
    /// begin count
    /// </summary>
    /// <param name="service"> the service </param>
    public static void BeginCount(string service)
    {
        GetStatus(service).active.IncrementAndGet();
    }

    /// <summary>
    /// end count
    /// </summary>
    /// <param name="service"> the service </param>
    public static void EndCount(string service)
    {
        RpcStatus rpcStatus = GetStatus(service);
        rpcStatus.active.DecrementAndGet();
        rpcStatus.total.IncrementAndGet();
    }

    /// <summary>
    /// set value.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Set(string key, object value)
    {
        values[key]= value;
    }

    /// <summary>
    ///  get value.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object Get(string key)
    {
        return values[key];
    }

    /// <summary>
    /// get active.
    /// </summary>
    /// <returns>active</returns>
    public int GetActive()
    {
        return active.Value;
    }


    /// <summary>
    /// get total.
    /// </summary>
    /// <returns>total</returns>
    public long GetTotal()
    {
        return total.Value;
    }

    /// <summary>
    ///  get total elapsed.
    /// </summary>
    /// <returns>total elapsed</returns>
    public long GetTotalElapsed()
    {
        return totalElapsed.Value;
    }

    /// <summary>
    /// get average elapsed.
    /// </summary>
    /// <returns>average elapsed</returns>
    public long GetAverageElapsed()
    {
        long total = GetTotal();
        if (total == 0)
        {
            return 0;
        }
        return GetTotalElapsed() / total;
    }

    /// <summary>
    /// get max elapsed.
    /// </summary>
    /// <returns>max elapsed</returns>
    public long GetMaxElapsed()
    {
        return maxElapsed.Value;
    }

    /// <summary>
    /// get failed.
    /// </summary>
    /// <returns>failed</returns>
    public int GetFailed()
    {
        return failed.Value;
    }

    /// <summary>
    /// get failed elapsed.
    /// </summary>
    /// <returns>failed elapsed</returns>
    public long GetFailedElapsed()
    {
        return failedElapsed.Value;
    }

    /// <summary>
    /// get failed average elapsed.
    /// </summary>
    /// <returns>failed average elapsed</returns>
    public long GetFailedAverageElapsed()
    {
        long failed = GetFailed();
        if (failed == 0)
        {
            return 0;
        }
        return GetFailedElapsed() / failed;
    }

    /// <summary>
    /// get failed max elapsed.
    /// </summary>
    /// <returns>failed max elapsed</returns>
    public long GetFailedMaxElapsed()
    {
        return failedMaxElapsed.Value;
    }

    /// <summary>
    /// get succeeded.
    /// </summary>
    /// <returns>succeeded</returns>
    public long GetSucceeded()
    {
        return GetTotal() - GetFailed();
    }

    /// <summary>
    /// get succeeded elapsed.
    /// </summary>
    /// <returns>succeeded elapsed</returns>
    public long GetSucceededElapsed()
    {
        return GetTotalElapsed() - GetFailedElapsed();
    }

    /// <summary>
    /// get succeeded average elapsed.
    /// </summary>
    /// <returns>succeeded average elapsed</returns>
    public long GetSucceededAverageElapsed()
    {
        long succeeded = GetSucceeded();
        if (succeeded == 0)
        {
            return 0;
        }
        return GetSucceededElapsed() / succeeded;
    }

    /// <summary>
    /// get succeeded max elapsed.
    /// </summary>
    /// <returns>succeeded max elapsed</returns>
    public long GetSucceededMaxElapsed()
    {
        return succeededMaxElapsed.Value;
    }

    /// <summary>
    /// Calculate average TPS (Transaction per second)
    /// </summary>
    /// <returns></returns>
    public long GetAverageTps()
    {
        if (GetTotalElapsed() >= 1000L)
        {
            return GetTotal() / (GetTotalElapsed() / 1000L);
        }
        return GetTotal();
    }

    /// <summary>
    /// 获取限制线程数的信号量，信号量的许可数就是executes设置的值
    /// </summary>
    /// <param name="maxThreadNum"></param>
    /// <returns></returns>
    public Semaphore? GetSemaphore(int maxThreadNum)
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
