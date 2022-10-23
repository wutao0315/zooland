using Zooyard.Diagnositcs;
using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

public class FailsafeCluster : AbstractCluster
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(FailsafeCluster));
    //public FailsafeCluster(IEnumerable<ICache> caches) : base(caches) { }
    public override string Name => NAME;
    public const string NAME = "failsafe";


    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> invokers, IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        Exception? exception = null;
        CheckInvokers(invokers, invocation, address);

        ////路由
        //var invokers = base.Route(urls, address, invocation);

        var invoker = base.Select(loadbalance, invocation, invokers, null);
        try
        {
            var client = await pool.GetClient(invoker);
            try
            {
                var refer = await client.Refer();
                _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                var result = await refer.Invoke<T>(invocation);
                _source.WriteConsumerAfter(invoker, invocation, result);
                await pool.Recovery(client);
                goodUrls.Add(invoker);
                return new ClusterResult<T>(result, goodUrls, badUrls, exception, false);
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
            exception = e;
            badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
            Logger().LogError(e, $"Failsafe ignore exception: {e.Message}");
            var result = new RpcResult<T>(e); // ignore
            return new ClusterResult<T>(result, goodUrls, badUrls, exception, false);
        }
    }
}
