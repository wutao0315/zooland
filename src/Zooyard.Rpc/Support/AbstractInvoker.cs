using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractInvoker<T> : IInvoker
    {
        public abstract void Dispose();
        public virtual object Instance { get; }
        public abstract IResult Invoke(IInvocation invocation);
    }
}
