namespace Zooyard.Rpc;

public interface IExporter<T>
{
    IInvoker Invoker { get; }
    void UnExport();
}
