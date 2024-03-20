using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.ExceptionServices;
using Zooyard.DataAnnotations;
using Zooyard.DynamicProxy;
using Zooyard.Rpc;
using Zooyard.Utils;

namespace Zooyard;
public class ZooyardInvoker
{
    private readonly ILogger _logger;
    private readonly IZooyardPools _zooyardPools;
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly ZooyardAttribute _zooyardAttribute;
    public ZooyardInvoker(ILogger logger, IZooyardPools zooyardPools, IEnumerable<IInterceptor> interceptors, ZooyardAttribute? zooyardAttribute) 
    {
        _logger = logger;
        _zooyardPools = zooyardPools;
        _interceptors = interceptors;
        if (zooyardAttribute == null) 
        {
            throw new RpcException($"{nameof(ZooyardAttribute)} is not exists");
        }
        _zooyardAttribute = zooyardAttribute;
    }

    //public IDictionary<MethodInfo, int> GetMethodPosition(Type declaringType)
    //{
    //    var methodToToken = new Dictionary<MethodInfo, int>(MethodInfoEqualityComparer.Instance);
    //    var methods = declaringType.GetRuntimeMethods();
    //    foreach (var method in methods)
    //    {
    //        if (!methodToToken.TryGetValue(method, out _))
    //        {
    //            var token = methodToToken.Count;
    //            methodToToken[method] = token;
    //        }
    //    }
    //    return methodToToken;
    //}

    public (MethodInfo, int) GetInterfaceMethod(StackTrace stackTrace, InterfaceMapping interfaceMapping)
    {
        foreach (var frames in stackTrace.GetFrames())
        {
            var md = frames.GetMethod();
            if (md == null || md is not MethodInfo method)
            {
                continue;
            }

            if (method.GetCustomAttribute<ZooyardImplAttribute>() == null)
            {
                continue;
            }
            var index = Array.IndexOf(interfaceMapping.TargetMethods, md);

            if (index == -1) 
            {
                throw new ArgumentNullException($"index not exists{md}");
            }

            var im = interfaceMapping.InterfaceMethods[index];

            return (im, index);
        }
        throw new ArgumentNullException("not a impl method");
    }

    // object[] args = new object[paramCount];
    // object[] packed = new object[PackedArgs.PackedTypes.Length];
    // packed[PackedArgs.DispatchProxyPosition] = this;
    // packed[PackedArgs.DeclaringTypePosition] = typeof(iface);
    // packed[PackedArgs.MethodTokenPosition] = iface method token;
    // packed[PackedArgs.ArgsPosition] = args;
    // packed[PackedArgs.GenericTypesPosition] = mi.GetGenericArguments();
    // Call AsyncDispatchProxyGenerator.Invoke(object[]), InvokeAsync or InvokeAsyncT
    public ProxyMethodResolverContext GetMethodResolverContext(ProxyExecutor obj, Type declaringType, MethodInfo mi, int mtoken, object[] args) 
    {
        object[] packed = new object[PackedArgs.PackedTypes.Length];
        packed[PackedArgs.DispatchProxyPosition] = obj;
        packed[PackedArgs.DeclaringTypePosition] = declaringType;
        packed[PackedArgs.MethodTokenPosition] = mtoken;
        packed[PackedArgs.ArgsPosition] = args;
        packed[PackedArgs.GenericTypesPosition] = mi.GetGenericArguments();
        var packedArg = new PackedArgs(packed);
        var context = new ProxyMethodResolverContext(packedArg, mi);
        return context;
    }

    public object? Invoke(ProxyMethodResolverContext context)
    {
        var returnValue = AsyncHelper.RunSync(() => InvokeAsync<object>(context));
        return returnValue;
    }
    public TT? Invoke<TT>(ProxyMethodResolverContext context)
    {
        var returnValue = AsyncHelper.RunSync(() => InvokeAsync<TT>(context));
        return returnValue;
    }

    public async Task InvokeAsync(ProxyMethodResolverContext context)
    {
        await InvokeAsync<object>(context);
    }

    public async Task<TT?> InvokeAsync<TT>(ProxyMethodResolverContext context)
    {
        var watch = Stopwatch.StartNew();

        var attribute = context.Method.GetCustomAttribute<DefaultValueAttribute>();
        if (attribute == null)
        {
            attribute = context.Packed.DeclaringType.GetCustomAttribute<DefaultValueAttribute>();
        }

        TT? returnValue = default;
        try
        {
            var icn = new RpcInvocation(Guid.NewGuid().ToString("N"), _zooyardAttribute.ServiceName, _zooyardAttribute.Version, _zooyardAttribute.Url, context.Packed.DeclaringType, context.Method, context.Packed.Args);
            var rpcContext = RpcContext.GetContext();
            rpcContext.SetInvocation(icn);

            //todo before invoke
            if (_interceptors != null && _interceptors.Count() > 0)
            {
                foreach (var item in _interceptors)
                {
                    await item.BeforeCall(icn, rpcContext);
                }
            }

            var result = await _zooyardPools.Invoke<TT>(icn);
            //todo after invoke
            if (_interceptors != null && _interceptors.Count() > 0)
            {
                foreach (var item in _interceptors)
                {
                    await item.AfterCall(icn, rpcContext, out result);
                }
            }

            if (result != null)
            {
                returnValue = result.Value;
                context.Packed.ReturnValue = returnValue;
            }
        }
        catch (Exception e)
        {
            if (attribute == null)
            {
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            else
            {
                returnValue = (TT?)attribute.Value;
                context.Packed.ReturnValue = returnValue;
            }
        }
        finally
        {
            watch.Stop();
        }
        _logger.LogInformation("{0} call {1} async proxy generator: {2} ms", context.Packed.DeclaringType, context.Method, watch.ElapsedMilliseconds);
        return returnValue;
    }

}
