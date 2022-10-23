using System.Collections.Concurrent;
using System.Diagnostics;
using Zooyard.Diagnositcs;
using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

/// <summary>
/// 失败自动恢复，后台记录失败请求，定时重发，通常用于消息通知操作。
/// </summary>
public class FailbackCluster : AbstractCluster
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(FailbackCluster));
    //public FailbackCluster(IEnumerable<ICache> caches) : base(caches) { }
    public override string Name => NAME;
    public const string NAME = "failback";
    private static readonly long RETRY_FAILED_PERIOD = 5 * 1000;

    private ConcurrentDictionary<IInvocation, URL> failed = new ();
    private System.Timers.Timer? retryTimer;


    private void AddFailed<T>(IClientPool pool,IInvocation invocation, URL router)
    {
       
        if (retryTimer == null)
        {
            lock (this)
            {
                if (retryTimer == null)
                {
                    retryTimer = new System.Timers.Timer(RETRY_FAILED_PERIOD);
                    retryTimer.Elapsed += new System.Timers.ElapsedEventHandler(async (object? sender, System.Timers.ElapsedEventArgs events) =>
                    {
                        // 收集统计信息
                        try
                        {
                            await RetryFailed<T>(pool).ConfigureAwait(false);
                        }
                        catch (Exception t)
                        { // 防御性容错
                            Logger().LogError(t, $"Unexpected error occur at collect statistic {t.Message}");
                        }
                    });
                    retryTimer.AutoReset = true;
                    retryTimer.Enabled = true;
                }
            }
        }

        failed.TryAdd(invocation, router);
    }



    async Task RetryFailed<T>(IClientPool pool)
    {
        if (failed.Count == 0)
        {
            return;
        }

        foreach (var invocation in failed.Keys)
        {
            var client = await pool.GetClient(failed[invocation]);
            try
            {
                var refer = await client.Refer();
                _source.WriteConsumerBefore(refer.Instance, failed[invocation], invocation);
                var result = await refer.Invoke<T>(invocation);
                _source.WriteConsumerAfter(failed[invocation], invocation, result);
                await pool.Recovery(client);
                failed.TryRemove(invocation, out URL? cluster);
            }
            catch (Exception e)
            {
                _source.WriteConsumerError(failed[invocation], invocation, e);
                await pool.DestoryClient(client);
                Logger().LogError(e, $"Failed retry to invoke method {invocation.MethodInfo.Name}, waiting again.");
            }
        }
    }

    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> invokers, IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        Exception? exception = null;

        CheckInvokers(invokers, invocation, address);

        var invoker = base.Select(loadbalance, invocation, invokers);

        IResult<T> result;
        var watch = Stopwatch.StartNew();
        try
        {
            var client = await pool.GetClient(invoker);
            try
            {
                var refer = await client.Refer();
                _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                result = await refer.Invoke<T>(invocation);
                _source.WriteConsumerAfter(invoker, invocation, result);
                await pool.Recovery(client);
                goodUrls.Add(invoker);
            }
            catch (Exception ex)
            {
                await pool.DestoryClient(client).ConfigureAwait(false);
                _source.WriteConsumerError(invoker, invocation, ex);
                throw;
            }
        }
        catch (Exception e)
        {
            Logger().LogError(e, $"Failback to invoke method {invocation.MethodInfo.Name}, wait for retry in background. Ignored exception:{e.Message}");
            AddFailed<T>(pool, invocation, invoker);
            watch.Stop();
            result = new RpcResult<T>(watch.ElapsedMilliseconds); // ignore
            exception = e;
            badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
        }
        finally 
        {
            if(watch.IsRunning)
                watch.Stop();
        }

        return new ClusterResult<T>(result, goodUrls, badUrls, exception, false);
    }
}
