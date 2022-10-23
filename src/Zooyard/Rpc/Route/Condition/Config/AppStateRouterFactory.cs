using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition.Config;

public class AppStateRouterFactory : IStateRouterFactory
{
    public const string NAME = "app";

    public string Name => NAME;

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
        return new AppStateRouter(address, address.GetParameter("rule", "application"));
    }
}
