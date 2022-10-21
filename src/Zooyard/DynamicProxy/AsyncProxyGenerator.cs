using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Zooyard.Logging;

namespace Zooyard.DynamicProxy;

public class AsyncProxyGenerator : IDisposable
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(AsyncProxyGenerator));
    private readonly ConcurrentDictionary<Type, Dictionary<Type, Type>> _proxyTypeCaches;

    private readonly ProxyAssembly _proxyAssembly;

    //private readonly MethodInfo _dispatchProxyInvokeMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("Invoke");
    //private readonly MethodInfo _dispatchProxyInvokeAsyncMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("InvokeAsync");
    //private readonly MethodInfo _dispatchProxyInvokeAsyncTMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("InvokeAsyncT");


    private readonly IZooyardPools _zooyardPools;
    private readonly string _serviceName;
    private readonly string _version;
    private readonly string _url;

    public AsyncProxyGenerator(IZooyardPools zooyardPools, string serviceName, string version, string url)
    {
        _proxyTypeCaches = new ConcurrentDictionary<Type, Dictionary<Type, Type>>();
        _proxyAssembly = new ProxyAssembly();
        _zooyardPools = zooyardPools;
        _serviceName = serviceName;
        _version = version;
        _url = url;
    }
    /// <summary> 创建代理 </summary>
    /// <param name="interfaceType"></param>
    /// <returns></returns>
    public object CreateProxy(Type interfaceType)
    {
        var proxiedType = GetProxyType(typeof(ProxyExecutor), interfaceType);
        return Activator.CreateInstance(proxiedType, new ProxyHandler(this))!;
    }

    /// <summary> 创建代理 </summary>
    /// <param name="interfaceType"></param>
    /// <param name="baseType"></param>
    /// <param name="zooyardPools"></param>
    /// <param name="app"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public object CreateProxy(Type interfaceType, Type baseType, IZooyardPools zooyardPools, string app, string version)
    {
        var proxiedType = GetProxyType(baseType, interfaceType);
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
            interfaceToProxy = new Dictionary<Type, Type>();
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
        var method = _proxyAssembly.ResolveMethodToken(packed.DeclaringType, packed.MethodToken);
        if (method.IsGenericMethodDefinition)
            method = method.MakeGenericMethod(packed.GenericTypes);

        return new ProxyMethodResolverContext(packed, method);
    }

    public object? Invoke(object[] args)
    {
        var returnValue = InvokeAsync<object>(args).GetAwaiter().GetResult();
        return returnValue;
    }
    public T? Invoke<T>(object[] args)
    {
        var returnValue = InvokeAsync<T>(args).GetAwaiter().GetResult();
        return returnValue;
    }

    public async Task InvokeAsync(object[] args)
    {
        await InvokeAsync<object>(args);
    }

    public async Task<T?> InvokeAsync<T>(object[] args)
    {
        var watch = Stopwatch.StartNew();
        var context = Resolve(args);

        T? returnValue = default;
        try
        {
            //Debug.Assert(_dispatchProxyInvokeAsyncTMethod != null);
            //var genericmethod = _dispatchProxyInvokeAsyncTMethod.MakeGenericMethod(typeof(T));
            //returnValue = await (Task<T>)genericmethod.Invoke(context.Packed.DispatchProxy,
            //                                                       new object[] { context.Method, context.Packed.Args });
            var icn = new RpcInvocation(_serviceName, _version, _url, context.Packed.DeclaringType, context.Method, context.Packed.Args);
            var result = await _zooyardPools.Invoke<T>(icn);
            if (result!=null) 
            {
                returnValue = result.Value;
                context.Packed.ReturnValue = returnValue;
            }
        }
        catch (TargetInvocationException tie)
        {
            ExceptionDispatchInfo.Capture(tie).Throw();
        }
        finally
        {
            watch.Stop();
        }
        Logger().LogInformation($"async proxy generator: {watch.ElapsedMilliseconds} ms");
        return returnValue;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
