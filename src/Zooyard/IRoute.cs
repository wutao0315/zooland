namespace Zooyard;

public interface IRoute
{
    URL GetUrl();
    List<IInvoker> Route(List<IInvoker> invokers, URL url, IInvocation invocation);
    int GetPriority();
}
