using Microsoft.Extensions.Logging;
using Zooyard.Management;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Tag;

public class TagStateRouterFactory(ILoggerFactory _loggerFactory, IRpcStateLookup _stateLookup) : CacheableStateRouterFactory
{
    public const string NAME = "tag";
    public override string Name => NAME;
    protected override IStateRouter CreateRouter(Type interfaceClass, URL address)
    {
        return new TagStateRouter(_loggerFactory, _stateLookup, address);
    }

}
