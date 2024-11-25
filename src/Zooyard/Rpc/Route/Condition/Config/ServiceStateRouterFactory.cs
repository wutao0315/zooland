using Microsoft.Extensions.Logging;
using Zooyard.Management;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition.Config;

public class ServiceStateRouterFactory(ILoggerFactory _loggerFactory, IRpcStateLookup _stateLookup) : CacheableStateRouterFactory
{
    public const string NAME = "service";
    public override string Name => NAME;
    //private readonly ILoggerFactory _loggerFactory;
    //private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    //public ServiceStateRouterFactory(ILoggerFactory loggerFactory, IOptionsMonitor<ZooyardOption> zooyard)
    //{
    //    _loggerFactory = loggerFactory;
    //    _zooyard = zooyard;
    //}

    protected override IStateRouter CreateRouter(Type interfaceClass, URL url)
    {
        return new ServiceStateRouter(_loggerFactory, _stateLookup, url);
    }
}
