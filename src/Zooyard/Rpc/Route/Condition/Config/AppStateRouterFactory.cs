using Microsoft.Extensions.Logging;
using Zooyard.Management;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition.Config;

public class AppStateRouterFactory(ILoggerFactory _loggerFactory, IRpcStateLookup _stateLookup) : IStateRouterFactory
{
    public const string NAME = "app";

    public string Name => NAME;
    //private readonly ILoggerFactory _loggerFactory;
    //private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    //public AppStateRouterFactory(ILoggerFactory loggerFactory, IOptionsMonitor<ZooyardOption> zooyard)
    //{
    //    _loggerFactory = loggerFactory;
    //    _zooyard = zooyard;
    //}
    public void ClearCache() { }

    private volatile IStateRouter? router;

    public IStateRouter GetRouter(Type interfaceClass, URL address)
    {
        if (router != null)
        {
            return router;
        }
        lock (this)
        {
            router ??= CreateRouter(address);
        }
        return router;
    }

    private IStateRouter CreateRouter(URL address)
    {
        return new AppStateRouter(_loggerFactory, _stateLookup, address, address.GetParameter("rule", "application"));
    }
}
