using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Tag;

public class TagStateRouterFactory : CacheableStateRouterFactory
{
    public const string NAME = "tag";
    public override string Name => NAME;
    protected override IStateRouter CreateRouter(Type interfaceClass, URL address)
    {
        return new TagStateRouter(address);
    }

}
