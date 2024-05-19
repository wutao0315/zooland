using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Zooyard.Diagnositcs;
//using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

public class FailsafeCluster : AbstractCluster
{
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(FailsafeCluster));
    //public FailsafeCluster(IEnumerable<ICache> caches) : base(caches) { }
    public FailsafeCluster(ILogger<FailsafeCluster> logger) : base(logger) { }
    public override string Name => NAME;
    public const string NAME = "failsafe";


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

        var watch = Stopwatch.StartNew();
        try
        {
            var client = await pool.GetClient(invoker);

            try
            {
                var refer = await client.Refer();
                _source.WriteConsumerBefore(client.System, Name, invoker, invocation);
                var result = await refer.Invoke<T>(invocation);
                result.ElapsedMilliseconds = watch.ElapsedMilliseconds;
                _source.WriteConsumerAfter(client.System, Name, invoker, invocation, result);
                await pool.Recovery(client);
                goodUrls.Add(invoker);
                return new ClusterResult<T>(result, 
                    goodUrls, badUrls,
                    exception, false);
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
            exception = e;
            //badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
            var badUrl = badUrls.FirstOrDefault(w => w.Url == invoker);
            if (badUrl != null)
            {
                badUrl.BadTime = DateTime.Now;
                badUrl.CurrentException = e;
            }
            else
            {
                badUrls.Add(new BadUrl(invoker, e));
            }
            _logger.LogError(e, $"Failsafe ignore exception: {e.Message}");
            var result = new RpcResult<T>(e) { ElapsedMilliseconds = watch.ElapsedMilliseconds }; // ignore
            return new ClusterResult<T>(result, 
                goodUrls, badUrls, 
                exception, false);
        }
        finally
        {
            watch.Stop();
        }
    }
}
