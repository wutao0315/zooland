using Zooyard.Diagnositcs;
using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

public class FailsafeCluster : AbstractCluster
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(FailsafeCluster));
    public override string Name => NAME;
    public const string NAME = "failsafe";


    public override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        Exception exception = null;
        checkInvokers(urls, invocation, address);
        var invoker = base.select(loadbalance, invocation, urls, null);
        IResult<T> result;
        try
        {
            var client =await pool.GetClient(invoker);
            try
            {
                var refer = await client.Refer();
                _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                result = await refer.Invoke<T>(invocation);
                _source.WriteConsumerAfter(invoker, invocation, result);
                await pool.Recovery(client);
                goodUrls.Add(invoker);
                return new ClusterResult<T>(result, goodUrls, badUrls, exception, false);
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
            exception = e;
            badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
            Logger().LogError(e, $"Failsafe ignore exception: {e.Message}");
            result = new RpcResult<T>(e); // ignore
        }
        return new ClusterResult<T>(result, goodUrls, badUrls,exception,false);
    }
}
