using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition.Config;

public class ServiceStateRouterFactory : CacheableStateRouterFactory
{
    public const string NAME = "service";

    public override string Name => NAME;

    protected override IStateRouter CreateRouter(Type interfaceClass, URL url)
    {
        return new ServiceStateRouter(url);
    }
}
