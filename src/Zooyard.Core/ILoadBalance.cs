using System.Collections.Generic;

namespace Zooyard.Core
{
    public interface ILoadBalance
    {
       URL Select(IList<URL> urls,IInvocation invocation);
    }
}
