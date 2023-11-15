using Microsoft.Extensions.Logging;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition;

internal class ConditionStateRouterFactory : CacheableStateRouterFactory
{
    private readonly ILoggerFactory _loggerFactory;
    public ConditionStateRouterFactory(ILoggerFactory loggerFactory) 
    {
        _loggerFactory = loggerFactory;
    }
    public const string NAME = "condition";
    public override string Name => NAME;
    protected override IStateRouter CreateRouter(Type interfaceClass, URL address)
    {
        return new ConditionStateRouter(_loggerFactory, address);
    }
}
