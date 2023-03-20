using Microsoft.Extensions.Options;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition.Config;

public class ServiceStateRouterFactory : CacheableStateRouterFactory
{
    public const string NAME = "service";

    public override string Name => NAME;
    private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    public ServiceStateRouterFactory(IOptionsMonitor<ZooyardOption> zooyard)
    {
        _zooyard = zooyard;
    }

    protected override IStateRouter CreateRouter(Type interfaceClass, URL url)
    {
        return new ServiceStateRouter(_zooyard, url);
    }
}
