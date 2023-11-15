using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using Zooyard.Diagnositcs;
//using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

/// <summary>
/// 失败自动恢复，后台记录失败请求，定时重发，通常用于消息通知操作。
/// </summary>
public class FailbackCluster : AbstractCluster
{
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(FailbackCluster));
    //public FailbackCluster(IEnumerable<ICache> caches) : base(caches) { }
    public FailbackCluster(ILogger<FailbackCluster> logger) : base(logger) { }
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
                            _logger.LogError(t, $"Unexpected error occur at collect statistic {t.Message}");
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
            var watch = Stopwatch.StartNew();
            try
            {
                var refer = await client.Refer();
                _source.WriteConsumerBefore(client.System, Name, failed[invocation], invocation);
                var result = await refer.Invoke<T>(invocation);
                watch.Stop();
                result.ElapsedMilliseconds = watch.ElapsedMilliseconds;
                _source.WriteConsumerAfter(client.System, Name, failed[invocation], invocation, result);
                await pool.Recovery(client);
                failed.TryRemove(invocation, out URL? cluster);
            }
            catch (Exception e)
            {
                _source.WriteConsumerError(client.System, Name, failed[invocation], invocation, e, watch.ElapsedMilliseconds);
                await pool.DestoryClient(client);
                _logger.LogError(e, $"Failed retry to invoke method {invocation.MethodInfo.Name}, waiting again.");
            }
            finally
            {
                if (watch.IsRunning)
                    watch.Stop();
            }
        }
    }

    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool,
        ILoadBalance loadbalance, 
        URL address, 
        IList<URL> invokers,
        IList<BadUrl> disabledUrls,
        IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        Exception? exception = null;

        CheckInvokers(invokers, invocation, address);

        var invoker = base.Select(loadbalance, invocation, invokers, disabledUrls);

        IResult<T> result;
        var watch = Stopwatch.StartNew();
        try
        {
            var client = await pool.GetClient(invoker);

            try
            {
                var refer = await client.Refer();
                _source.WriteConsumerBefore(client.System, Name, invoker, invocation);
                result = await refer.Invoke<T>(invocation);
                result.ElapsedMilliseconds = watch.ElapsedMilliseconds;
                _source.WriteConsumerAfter(client.System, Name,invoker, invocation, result);
                await pool.Recovery(client);
                goodUrls.Add(invoker);
            }
            catch (Exception ex)
            {
                await pool.DestoryClient(client).ConfigureAwait(false);
                _source.WriteConsumerError(client.System, Name, invoker, invocation, ex, watch.ElapsedMilliseconds);
                throw;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failback to invoke method {invocation.MethodInfo.Name}, wait for retry in background. Ignored exception:{e.Message}");
            AddFailed<T>(pool, invocation, invoker);
            watch.Stop();
            result = new RpcResult<T>(e)
            {
                ElapsedMilliseconds = watch.ElapsedMilliseconds,
            }; // ignore
            exception = e;
            badUrls.Add(new BadUrl(invoker, exception));
        }
        finally
        {
            watch.Stop();
        }

        return new ClusterResult<T>(result, 
            goodUrls, badUrls, 
            exception, false);
    }
}
