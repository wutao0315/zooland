namespace Zooyard;

public interface IInvoker
{
    object Instance { get; }
    int ClientTimeout { get; }
    Task<IResult<T>> Invoke<T>(IInvocation invocation);
}
