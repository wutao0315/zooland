using Microsoft.Extensions.Logging;
using Zooyard.DataAnnotations;
using Zooyard.DynamicProxy;

namespace Zooyard;

public class ZooyardFactory<T>(
    ILoggerFactory _loggerFactory, 
    IZooyardPools _pools, 
    IEnumerable<IInterceptor> _interceptors, 
    ZooyardAttribute? zooyardAttribute)
{
    public T CreateYard()
    {
        var invoker = new ZooyardInvoker(_loggerFactory.CreateLogger<T>(),  _pools, _interceptors, zooyardAttribute);
        var proxyGenerator = new AsyncProxyGenerator(invoker);
        return (T)proxyGenerator.CreateProxy(typeof(T));
    }
}
