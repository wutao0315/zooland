using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractExporter<T> : IExporter<T>
    {
        public abstract IInvoker Invoker { get; }

        public abstract void UnExport();
    }
}
