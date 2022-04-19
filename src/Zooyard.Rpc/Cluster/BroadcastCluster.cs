using Zooyard.Diagnositcs;
using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

public class BroadcastCluster : AbstractCluster
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(BroadcastCluster));
    public override string Name => NAME;
    public const string NAME = "broadcast";
    public override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
    {
        checkInvokers(urls, invocation, address);
        RpcContext.GetContext().SetInvokers(urls);
        Exception exception = null;
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        var isThrow = false;
        IResult<T> result = null;
        foreach (var invoker in urls)
        {
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
                    await pool.DestoryClient(client);
                    _source.WriteConsumerError(invoker,invocation ,ex);
                    throw ex;
                }
            }
            catch (Exception e)
            {
                exception = e;
                badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
                Logger().LogWarning(e, e.Message);
            }
        }
        if (exception != null)
        {
            isThrow = true;
        }
        var clusterResult = new ClusterResult<T>(result, goodUrls, badUrls, exception, isThrow);
        return clusterResult;
    }
}
