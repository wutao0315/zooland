using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Mock;

public class MockStateRouterFactory : IStateRouterFactory
{
    public const string NAME = "mock";
    public string Name => NAME;
    public IStateRouter GetRouter(Type interfaceClass, URL address)
    {
        return new MockStateRouter(address);
    }
}
