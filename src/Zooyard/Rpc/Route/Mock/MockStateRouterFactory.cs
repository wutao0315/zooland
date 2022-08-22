using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Mock;

public class MockStateRouterFactory<T> : IStateRouterFactory<T>
{
    public const string NAME = "mock";

    public IStateRouter<T> GetRouter(Type interfaceClass, URL url)
    {
        return new MockInvokersSelector<T>(url);
    }
}
