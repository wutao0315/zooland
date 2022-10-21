using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Tag;

public class TagStateRouterFactory<T> : CacheableStateRouterFactory<T>
{
    public const string NAME = "tag";

protected override IStateRouter<T> createRouter(Type interfaceClass, URL url)
    {
        return new TagStateRouter<T>(url);
    }
  
}
