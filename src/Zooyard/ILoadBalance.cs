namespace Zooyard;

public interface ILoadBalance
{
    string Name { get; }
    URL Select(IList<URL> urls,IInvocation invocation);
}
