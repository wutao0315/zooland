using System.Collections.Concurrent;
using System.Reflection;

namespace Zooyard.DynamicProxy;

public class AsyncProxyGenerator : IDisposable
{
    private readonly ConcurrentDictionary<Type, Dictionary<Type, Type>> _proxyTypeCaches;
    private readonly ProxyAssembly _proxyAssembly;
    private readonly ZooyardInvoker _zooyardInvoker;

    public AsyncProxyGenerator(ZooyardInvoker zooyardInvoker)
    {
        _proxyTypeCaches = new ConcurrentDictionary<Type, Dictionary<Type, Type>>();
        _proxyAssembly = new ProxyAssembly();

        _zooyardInvoker = zooyardInvoker;
    }
    /// <summary> 创建代理 </summary>
    /// <param name="interfaceType"></param>
    /// <returns></returns>
    public object CreateProxy(Type interfaceType)
    {
        var proxiedType = GetProxyType(typeof(ProxyExecutor), interfaceType);
        return Activator.CreateInstance(proxiedType, new ProxyHandler(this))!;
    }

    /// <summary> 获取代理类型 </summary>
    /// <param name="baseType"></param>
    /// <param name="interfaceType"></param>
    /// <returns></returns>
    private Type GetProxyType(Type baseType, Type interfaceType)
    {
        if (!_proxyTypeCaches.TryGetValue(baseType, out var interfaceToProxy))
        {
            interfaceToProxy = [];
            _proxyTypeCaches[baseType] = interfaceToProxy;
        }

        if (!interfaceToProxy.TryGetValue(interfaceType, out var generatedProxy))
        {
            generatedProxy = GenerateProxyType(baseType, interfaceType);
            interfaceToProxy[interfaceType] = generatedProxy;
        }

        return generatedProxy;
    }

    /// <summary> 生成代理类型 </summary>
    /// <param name="baseType"></param>
    /// <param name="interfaceType"></param>
    /// <returns></returns>
    private Type GenerateProxyType(Type baseType, Type interfaceType)
    {
        var baseTypeInfo = baseType.GetTypeInfo();
        if (!interfaceType.GetTypeInfo().IsInterface)
        {
            throw new ArgumentException($"InterfaceType_Must_Be_Interface, {interfaceType.FullName}", nameof(interfaceType));
        }

        if (baseTypeInfo.IsSealed)
        {
            throw new ArgumentException($"BaseType_Cannot_Be_Sealed, {baseTypeInfo.FullName}", nameof(baseType));
        }

        if (baseTypeInfo.IsAbstract)
        {
            throw new ArgumentException($"BaseType_Cannot_Be_Abstract {baseType.FullName}", nameof(baseType));
        }

        var pb = _proxyAssembly.CreateProxy($"Zooyard.Proxy.{interfaceType.Namespace}_{interfaceType.Name}", baseType);

        foreach (var t in interfaceType.GetTypeInfo().ImplementedInterfaces)
            pb.AddInterfaceImpl(t);

        pb.AddInterfaceImpl(interfaceType);

        var generatedProxyType = pb.CreateType();
        return generatedProxyType;
    }

    private ProxyMethodResolverContext Resolve(object[] args)
    {
        var packed = new PackedArgs(args);
        var method = _proxyAssembly.ResolveMethodToken(packed.MethodToken);
        if (method.IsGenericMethodDefinition)
            method = method.MakeGenericMethod(packed.GenericTypes);

        return new ProxyMethodResolverContext(packed, method);
    }

    public object? Invoke(object[] args)
    {
        var context = Resolve(args);
        var returnValue = _zooyardInvoker.Invoke(context);
        return returnValue;
    }
    public T? Invoke<T>(object[] args)
    {
        var context = Resolve(args);
        var returnValue = _zooyardInvoker.Invoke<T>(context);
        return returnValue;
    }

    public async Task InvokeAsync(object[] args)
    {
        var context = Resolve(args);
        await _zooyardInvoker.InvokeAsync(context);
    }

    public async Task<T?> InvokeAsync<T>(object[] args)
    {
        var context = Resolve(args);
        var result = await _zooyardInvoker.InvokeAsync<T>(context);
        return result;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
