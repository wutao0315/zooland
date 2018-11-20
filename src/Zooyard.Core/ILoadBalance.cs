using System.Collections.Generic;

namespace Zooyard.Core
{
    public interface ILoadBalance
    {
        string Name { get; }
        URL Select(IList<URL> urls,IInvocation invocation);
    }
}
