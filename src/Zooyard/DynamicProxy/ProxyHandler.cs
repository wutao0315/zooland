namespace Zooyard.DynamicProxy;

public sealed class ProxyHandler(AsyncProxyGenerator _generator)
{
    public object? InvokeHandle(object[] args)
    {
        return _generator.Invoke(args);
    }

    public T? InvokeHandleT<T>(object[] args)
    {
        return _generator.Invoke<T>(args);
    }

    public Task InvokeAsyncHandle(object[] args)
    {
        return _generator.InvokeAsync(args);
    }

    public Task<T?> InvokeAsyncHandleT<T>(object[] args)
    {
        return _generator.InvokeAsync<T>(args);
    }
}
