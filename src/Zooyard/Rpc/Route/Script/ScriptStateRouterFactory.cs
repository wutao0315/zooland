using Microsoft.Extensions.Logging;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Script;

public class ScriptStateRouterFactory: IStateRouterFactory
{
    private readonly ILoggerFactory _loggerFactory;
    public ScriptStateRouterFactory(ILoggerFactory loggerFactory) 
    {
        _loggerFactory = loggerFactory;
    }
    public const string NAME = "script";
    public string Name => NAME;
    public void ClearCache() { }
    public IStateRouter GetRouter(Type interfaceClass, URL address)
    {
        return new ScriptStateRouter(_loggerFactory, address);
    }
}
