using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.None;

public class NoneRouter : AbstractStateRouter
{
    public const string NAME = "NONE_ROUTER";

    public NoneRouter(URL address) : base(address) { }

    protected override IList<URL> DoRoute(IList<URL> invokers, URL url, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder, Holder<String> messageHolder)
    {
        return invokers;
    }
}
