namespace Zooyard.Core
{
    public interface IInvoker
    {
        object Instance { get; }
        IResult Invoke(IInvocation invocation);
    }
}
