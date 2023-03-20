using Microsoft.Extensions.Options;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Tag;

public class TagStateRouterFactory : CacheableStateRouterFactory
{
    private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    public TagStateRouterFactory(IOptionsMonitor<ZooyardOption> zooyard)
    {
        _zooyard = zooyard;
    }
    public const string NAME = "tag";
    public override string Name => NAME;
    protected override IStateRouter CreateRouter(Type interfaceClass, URL address)
    {
        return new TagStateRouter(_zooyard, address);
    }

}
