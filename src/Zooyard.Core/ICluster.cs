using System.Collections.Generic;

namespace Zooyard.Core
{
    public interface ICluster
    {
        IClusterResult DoInvoke(IClientPool pool,ILoadBalance loadbalance, URL address,IList<URL> urls, IInvocation invocation);
    }
}
