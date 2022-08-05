namespace Zooyard.Rpc;

public interface ILoadBalance
{
    string Name { get; }
    URL? Select(IList<URL>? urls, IInvocation invocation);
}
