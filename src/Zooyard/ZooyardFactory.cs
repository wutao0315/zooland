using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Zooyard.Attributes;
using Zooyard.DynamicProxy;
using Zooyard.Exceptions;

namespace Zooyard;

public class ZooyardFactory<T>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    private readonly ZooyardAttribute _zooyardAttribute;

    public ZooyardFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        
        var attribute = typeof(T).GetCustomAttribute<ZooyardAttribute>();
        if (attribute == null) 
        {
            throw new FrameworkException($"{typeof(T).FullName} mast have {typeof(ZooyardAttribute).FullName}");
        }

        _zooyardAttribute = attribute;
    }
    public T CreateYard()
    {
        var invoker = new ZooyardInvoker(_loggerFactory.CreateLogger<T>(), _serviceProvider, _zooyardAttribute);
        var proxyGenerator = new AsyncProxyGenerator(invoker);
        return (T)proxyGenerator.CreateProxy(typeof(T));
    }
}
