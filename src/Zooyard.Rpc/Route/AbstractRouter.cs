namespace Zooyard.Rpc.Route;

public abstract class AbstractRouter : IRoute
{
    public abstract int GetPriority();
    public abstract URL GetUrl();
    public abstract List<IInvoker> Route(List<IInvoker> invokers, URL url, IInvocation invocation);
}
