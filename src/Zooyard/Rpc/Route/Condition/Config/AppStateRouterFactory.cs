using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition.Config;

public class AppStateRouterFactory<T> : IStateRouterFactory<T>
{
    public const string NAME = "app";

    private volatile IStateRouter<T>? router;

    public IStateRouter<T> GetRouter(Type interfaceClass, URL url)
    {
        if (router != null)
        {
            return router;
        }
        lock (this)
        {
            if (router == null)
            {
                router = createRouter(url);
            }
        }
        return router;
    }

    private IStateRouter<T> createRouter(URL url)
    {
        return new AppStateRouter<T>(url);
    }
}
