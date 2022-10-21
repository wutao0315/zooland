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
    private readonly string _serviceName;
    /// <summary>
    /// api version
    /// </summary>
    private readonly string _version;
    /// <summary>
    /// default url
    /// </summary>
    private readonly string _url;

    public ZooyardFactory(IZooyardPools pools, string serviceName, string version, string url) 
    {
        _pools = pools;
        _serviceName = serviceName;
        _version = version;
        _url = url;
    }

    public T CreateYard()
    {
        var proxyGenerator = new AsyncProxyGenerator(_pools, _serviceName, _version, _url);
        return (T)proxyGenerator.CreateProxy(typeof(T));
    }
}
