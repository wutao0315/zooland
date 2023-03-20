using Microsoft.Extensions.Options;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition.Config;

public class AppStateRouterFactory : IStateRouterFactory
{
    public const string NAME = "app";

    public string Name => NAME;
    private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    public AppStateRouterFactory(IOptionsMonitor<ZooyardOption> zooyard)
    {
        _zooyard = zooyard;
    }
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
        return new AppStateRouter(_zooyard, address, address.GetParameter("rule", "application"));
    }
}
