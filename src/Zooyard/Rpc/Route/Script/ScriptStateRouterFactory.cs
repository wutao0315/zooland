using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Script;

public class ScriptStateRouterFactory<T>: IStateRouterFactory<T>
{
    public const string NAME = "script";

    public IStateRouter<T> GetRouter(Type interfaceClass, URL url)
    {
        return new ScriptStateRouter<T>(url);
    }
}
