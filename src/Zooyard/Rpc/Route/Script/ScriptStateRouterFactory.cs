using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Script;

public class ScriptStateRouterFactory: IStateRouterFactory
{
    public const string NAME = "script";
    public string Name => NAME;
    public void ClearCache() { }
    public IStateRouter GetRouter(Type interfaceClass, URL address)
    {
        return new ScriptStateRouter(address);
    }
}
