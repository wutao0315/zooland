namespace Zooyard.Core
{
    public interface IInvoker
    {
        IResult Invoke(IInvocation invocation);
    }
}
