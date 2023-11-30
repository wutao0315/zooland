using Microsoft.Extensions.Logging;
using Zooyard.DynamicProxy;

namespace Zooyard;

public class ZooyardFactory<T>(ILoggerFactory _loggerFactory, IZooyardPools _pools, string _serviceName, string _version, string _url)
{
    public T CreateYard()
    {
        var proxyGenerator = new AsyncProxyGenerator(_loggerFactory, _pools, _serviceName, _version, _url);
        return (T)proxyGenerator.CreateProxy(typeof(T));
    }
}
