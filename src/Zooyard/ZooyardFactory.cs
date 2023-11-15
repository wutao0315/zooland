using Microsoft.Extensions.Logging;
using Zooyard.DynamicProxy;

namespace Zooyard;

public class ZooyardFactory<T>
{
    private readonly ILoggerFactory _loggerFactory;
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

    public ZooyardFactory(ILoggerFactory loggerFactory, IZooyardPools pools, string serviceName, string version, string url) 
    {
        _loggerFactory = loggerFactory;
        _pools = pools;
        _serviceName = serviceName;
        _version = version;
        _url = url;
    }

    public T CreateYard()
    {
        var proxyGenerator = new AsyncProxyGenerator(_loggerFactory, _pools, _serviceName, _version, _url);
        return (T)proxyGenerator.CreateProxy(typeof(T));
    }
}
