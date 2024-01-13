using Microsoft.Extensions.Logging;
//using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

public class AvailableCluster : AbstractCluster
{
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(AvailableCluster));
    public AvailableCluster(ILogger<AvailableCluster> logger) : base(logger) { }
    public override string Name => NAME;
    public const string NAME = "available";

    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, 
        ILoadBalance loadbalance,
        URL address,
        IList<URL> invokers,
        IList<BadUrl> disabledUrls,
        IInvocation invocation) 
    {
        //foreach (var invoker in invokers)
        //{
        //    if (invoker.isAvailable())
        //    {
        //        return invokeWithContext(invoker, invocation);
        //    }
        //}
        //throw new RpcException("No provider available in " + invokers);
        //return new ClusterResult<T>(result,
        ////goodUrls, badUrls,
        //exception, isThrow);
        await Task.CompletedTask;
        return null!;
    }
}
