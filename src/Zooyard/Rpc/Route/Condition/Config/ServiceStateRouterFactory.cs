using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition.Config;

public class ServiceStateRouterFactory<T> : CacheableStateRouterFactory<T>
{
    public const string NAME = "service";

    protected override IStateRouter<T> createRouter(Type interfaceClass, URL url)
    {
        return new ServiceStateRouter<T>(url);
    }
}
