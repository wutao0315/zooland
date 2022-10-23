using Zooyard.Rpc.Route.Mock;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.None;

public class NoneStateRouterFactory : IStateRouterFactory
{
    public const string NAME = "none";
    public string Name => NAME;
    public IStateRouter GetRouter(Type interfaceClass, URL address)
    {
        return new NoneRouter(address);
    }
}
