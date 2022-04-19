using Zooyard.DynamicProxy;

namespace Zooyard;

public class ZooyardFactory<T>
{
    /// <summary>
    /// remoting service pools
    /// </summary>
    private readonly IZooyardPools _pools;
    /// <summary>
    /// application name
    /// </summary>
    private readonly string _app;
    /// <summary>
    /// api version
    /// </summary>
    private readonly string _version;

    public ZooyardFactory(IZooyardPools pools, string app, string version) 
    {
        _pools = pools;
        _app = app;
        _version = version;
    }

    public T CreateYard()
    {
        var proxyGenerator = new AsyncProxyGenerator(_pools, _app, _version);
        return (T)proxyGenerator.CreateProxy(typeof(T));
    }
}
