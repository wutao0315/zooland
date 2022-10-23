using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition;

internal class ConditionStateRouterFactory : CacheableStateRouterFactory
{
    public const string NAME = "condition";
    public override string Name => NAME;
    protected override IStateRouter CreateRouter(Type interfaceClass, URL address)
    {
        return new ConditionStateRouter(address);
    }
}
