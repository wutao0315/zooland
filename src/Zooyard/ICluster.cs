namespace Zooyard;

public interface ICluster
{
    string Name { get; }
    Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool,ILoadBalance loadbalance, URL address,IList<URL> urls, IInvocation invocation);
}
