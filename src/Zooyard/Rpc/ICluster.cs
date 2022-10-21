namespace Zooyard.Rpc;

public interface ICluster
{
    string Name { get; }
    Task<IClusterResult<T>> Invoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation);
}
