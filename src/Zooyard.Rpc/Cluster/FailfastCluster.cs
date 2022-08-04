using Zooyard.Diagnositcs;
using Zooyard.Logging;
using Zooyard.Utils;

namespace Zooyard.Rpc.Cluster;

public class FailfastCluster : AbstractCluster
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(FailfastCluster));

    public override string Name => NAME;
    public const string NAME = "failfast";


    public override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        IResult<T>? result = null;
        Exception? exception = null;
        var isThrow = false;

        CheckInvokers(urls, invocation, address);
        var invoker = base.Select(loadbalance, invocation, urls, null);

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
            }
            catch (Exception ex)
            {
                await pool.DestoryClient(client);
                _source.WriteConsumerError(invoker,invocation ,ex);
                throw;
            }
        }
        catch (Exception e)
        {
            isThrow = true;

            if (e is RpcException eBiz && eBiz.Biz)
            { 
                // biz exception.
                exception = e;
                //throw (RpcException)e;
            }
            else {

                exception = new RpcException(e is RpcException eRpc ? eRpc.Code : 0, "Failfast invoke providers "
                + invoker + " " + loadbalance.GetType().Name
                + " select from all providers " + string.Join(",", urls)
                + " for service " + invocation.TargetType.FullName
                + " method " + invocation.MethodInfo.Name
                + " on consumer " + Local.HostName
                + " use service version " + invocation.Version
                + ", but no luck to perform the invocation. Last error is: " + e.Message, e.InnerException ?? e);
            }
            Logger().LogError(exception, exception.Message);
            badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
        }

        return new ClusterResult<T>(result, goodUrls, badUrls, exception, isThrow);

    }
}
