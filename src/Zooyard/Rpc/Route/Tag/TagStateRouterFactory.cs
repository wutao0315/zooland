using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Tag;

public class TagStateRouterFactory : CacheableStateRouterFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    public TagStateRouterFactory(ILoggerFactory loggerFactory, IOptionsMonitor<ZooyardOption> zooyard)
    {
        _loggerFactory = loggerFactory;
        _zooyard = zooyard;
    }
    public const string NAME = "tag";
    public override string Name => NAME;
    protected override IStateRouter CreateRouter(Type interfaceClass, URL address)
    {
        return new TagStateRouter(_loggerFactory, _zooyard, address);
    }

}
