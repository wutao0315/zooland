using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Zooyard.Attributes;
using Zooyard.DynamicProxy;
using Zooyard.Exceptions;
using Zooyard.Rpc;
using Zooyard.Utils;

namespace Zooyard;
public class ZooyardInvoker
{
    private readonly ILogger _logger;
    private readonly IZooyardPools _zooyardPools;
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly ZooyardAttribute _zooyardAttribute;
    private readonly string _replacedUrl;
    private readonly string _replacedServiceName;
    public ZooyardInvoker(ILogger logger, IServiceProvider serviceProvider, ZooyardAttribute? zooyardAttribute)
    {
        if (zooyardAttribute == null)
        {
            throw new FrameworkException("rpc attribute not exits");
        }
        _logger = logger;
        _zooyardAttribute = zooyardAttribute;
        _zooyardPools = serviceProvider.GetRequiredService<IZooyardPools>(); ;
        _interceptors = serviceProvider.GetRequiredService<IEnumerable<IInterceptor>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        (_replacedUrl, _replacedServiceName) = GetConfigUrl(zooyardAttribute, configuration);
    }


    /// <summary>
    /// 获取配置数据
    /// </summary>
    /// <returns></returns>
    private (string, string) GetConfigUrl(ZooyardAttribute attr, IConfiguration configuration)
    {
        //如果协议是通用协议，检查配置中是否配置并替换
        var protocol = configuration.GetValue("zooyard:Protocol", "http")!;
        if (attr.Protocol.Equals("http", StringComparison.OrdinalIgnoreCase) && !attr.Protocol.Equals(protocol, StringComparison.OrdinalIgnoreCase))
        {
            attr.Protocol = protocol;
        }

        var url = attr.Url;
        var serviceName = attr.ServiceName;
        if (!string.IsNullOrWhiteSpace(attr.Config))
        {
            var keyValueList = attr.Config.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var kv in keyValueList)
            {
                var kvList = kv.Split('=');
                if (kvList.Length < 2)
                {
                    continue;
                }

                var cfgKeyValue = kvList[1].Split('@', StringSplitOptions.RemoveEmptyEntries);
                var cfgKey = cfgKeyValue[0];
                var cfgValue = "";
                if (cfgKeyValue.Length > 1)
                {
                    cfgValue = cfgKeyValue[1];
                }
                var val = configuration.GetValue(cfgKey, cfgValue);
                url = url.Replace($"{{{kvList[0]}}}", val);
                serviceName = serviceName.Replace($"{{{kvList[0]}}}", val);
            }
        }
        return (url, serviceName);
    }

    public IDictionary<MethodInfo, int> GetMethodPosition(Type declaringType)
    {
        var methodToToken = new Dictionary<MethodInfo, int>();
        var methods = declaringType.GetRuntimeMethods();
        foreach (var method in methods)
        {
            if (!methodToToken.TryGetValue(method, out var token))
            {
                token = methodToToken.Count;
                methodToToken[method] = token;
            }
        }
        return methodToToken;
    }

    ///// <summary>
    ///// get interface method from impl method body
    ///// </summary>
    ///// <param name="stackTrace"></param>
    ///// <param name="interfaceMapping"></param>
    ///// <returns></returns>
    ///// <exception cref="ArgumentNullException"></exception>
    //public (MethodInfo, int) GetInterfaceMethod(StackTrace stackTrace, InterfaceMapping interfaceMapping)
    //{
    //    foreach (var frames in stackTrace.GetFrames())
    //    {
    //        var md = frames.GetMethod();
    //        if (md == null || md is not MethodInfo method)
    //        {
    //            continue;
    //        }

    //        if (method.GetCustomAttribute<ZooyardImplAttribute>() == null)
    //        {
    //            continue;
    //        }
    //        var index = Array.IndexOf(interfaceMapping.TargetMethods, md);

    //        if (index == -1)
    //        {
    //            throw new ArgumentNullException($"index not exists{md}");
    //        }

    //        var im = interfaceMapping.InterfaceMethods[index];

    //        return (im, index);
    //    }
    //    throw new ArgumentNullException("not a impl method");
    //}



    /// <summary>
    /// get interface method from impl method body
    /// </summary>
    /// <param name="methodBase"></param>
    /// <param name="interfaceMapping"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public (MethodInfo, int) GetInterfaceMethodBase(MethodBase? methodBase, InterfaceMapping interfaceMapping)
    {
        if (methodBase == null)
        {
            throw new ArgumentNullException("method base is null");
        }

        if (methodBase is not MethodInfo md)
        {
            throw new ArgumentNullException($"method base is not a MethodInfo:{methodBase.GetType().FullName}");
        }

        var index = Array.IndexOf(interfaceMapping.TargetMethods, md);

        if (index == -1)
        {
            throw new ArgumentNullException($"index not exists{md}");
        }

        var im = interfaceMapping.InterfaceMethods[index];

        return (im, index);
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

    private AsyncLocal<Activity?> newActivityLOCAL = new();

    public async Task<TT?> InvokeAsync<TT>(ProxyMethodResolverContext context)
    {
        var watch = Stopwatch.StartNew();

        TT? returnValue = default;
        try
        {
            var url = _replacedUrl;

            //todo before invoke
            if (_interceptors != null && _interceptors.Count() > 0)
            {
                foreach (var item in _interceptors)
                {
                    url = await item.UrlCall(url, context);
                }
            }

            var icn = new RpcInvocation(Guid.NewGuid().ToString("N"), _zooyardAttribute, url, _replacedServiceName, context.Packed.DeclaringType, context.Method, context.Packed.Args);

            var targetDescription = icn.TargetType.GetCustomAttribute<ZooyardAttribute>();
            if (targetDescription != null)
            {
                foreach (var item in targetDescription.GetHeaders())
                {
                    icn.Headers[item.Key] = item.Value;
                }
                foreach (var item in targetDescription.GetMetadatas())
                {
                    icn.Metadatas[item.Key] = item.Value;
                }
                foreach (var item in targetDescription.GetParams())
                {
                    icn.Params[item.Key] = item.Value;
                }
            }

            var methodDescription = icn.MethodInfo.GetCustomAttribute<RequestMappingAttribute>();
            if (methodDescription != null)
            {
                foreach (var item in methodDescription.GetHeaders())
                {
                    icn.Headers[item.Key] = item.Value;
                }
                foreach (var item in methodDescription.GetMetadatas())
                {
                    icn.Metadatas[item.Key] = item.Value;
                }
                foreach (var item in methodDescription.GetParams())
                {
                    icn.Params[item.Key] = item.Value;
                }
            }

            var rpcContext = RpcContext.GetContext();
            rpcContext.SetInvocation(icn);

            var headers = new Dictionary<string, string?>();

            newActivityLOCAL.Value = null;
            Activity? activity = Activity.Current;

            if (activity == null)
            {
                newActivityLOCAL.Value = new Activity(ActivityName);
                newActivityLOCAL.Value.Start();
                InjectHeaders(newActivityLOCAL.Value, headers);

                foreach (var item in headers)
                {
                    icn.Headers[item.Key] = item.Value ?? "";
                    //rpcContext.SetAttachment(item.Key, item.Value ?? "");
                }
            }
            else
            {
                InjectHeaders(activity, headers);
                foreach (var item in headers)
                {
                    icn.Headers[item.Key] = item.Value ?? "";
                    //rpcContext.SetAttachment(item.Key, item.Value ?? "");
                }
            }

            //todo before invoke
            if (_interceptors != null && _interceptors.Count() > 0)
            {
                foreach (var item in _interceptors)
                {
                    await item.BeforeCall(icn, rpcContext);
                }
            }


            IResult<TT>? result = null;

            //制定返回类型父类
            var requstMapping = icn.MethodInfo.GetCustomAttribute<RequestMappingAttribute>();
            if (requstMapping != null && requstMapping.BaseReturnType != null && requstMapping.BaseReturnType != typeof(TT))
            {
                result = await BaseReturnCall<RequestMappingAttribute>(requstMapping.BaseReturnType);
            }
            else if (requstMapping != null && requstMapping.BaseReturnType != null && requstMapping.BaseReturnType == typeof(TT))
            {
                result = await ReturnCall<TT>();
            }
            else if (_zooyardAttribute.BaseReturnType != null && _zooyardAttribute.BaseReturnType != typeof(TT))
            {
                result = await BaseReturnCall<ZooyardAttribute>(_zooyardAttribute.BaseReturnType);
            }
            else if (_zooyardAttribute.BaseReturnType != null && _zooyardAttribute.BaseReturnType == typeof(TT))
            {
                result = await ReturnCall<TT>();
            }
            //全局设置基础返回父类
            else if (_zooyardPools.BaseReturnTypes != null 
                && _zooyardPools.BaseReturnTypes.TryGetValue(_zooyardAttribute.TypeName, out var baseReturnType) 
                && baseReturnType != typeof(TT))
            {
                result = await BaseReturnCall<ZooyardPools>(baseReturnType);
            }
            else 
            {
                result = await ReturnCall<TT>();
            }

            async Task<IResult<TT>?> BaseReturnCall<TA>(Type baseReturnType)
            {
                if (_zooyardAttribute.GetType().FullName == typeof(ZooyardHttpAttribute).FullName)
                {
                    var genericMethod = _zooyardPools.GetType().GetMethod(nameof(_zooyardPools.Invoke), 1, [typeof(IInvocation)])!;
                    if (!baseReturnType.IsGenericType)
                    {
                        throw new FrameworkException($"base return type {baseReturnType.FullName} at {typeof(TA).FullName} is not a Generic Type");
                    }

                    if (!typeof(IBaseReturnResult).IsAssignableFrom(baseReturnType))
                    {
                        throw new FrameworkException($"base return type {baseReturnType.FullName} not from {typeof(IBaseReturnResult).FullName} ");
                    }

                    var genericType = baseReturnType.MakeGenericType(typeof(TT));
                    var constructedMethod = genericMethod.MakeGenericMethod([genericType]);

                    object? resultObj = null;
                    try
                    {
                        // begin invoke
                        var taskObj = (Task)constructedMethod.Invoke(_zooyardPools, [icn])!;
                        await taskObj;
                        // end invoke

                        resultObj = taskObj.GetProperty<object>("Result");
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    newActivityLOCAL.Value?.Stop();
                    //todo after invoke
                    if (_interceptors != null && _interceptors.Count() > 0)
                    {
                        foreach (var item in _interceptors)
                        {
                            try
                            {
                                var afterGenericMethod = item.GetType().GetMethod(nameof(item.AfterCall))!;
                                var afterConstructedMethod = afterGenericMethod.MakeGenericMethod([genericType]);
                                var tObj = (Task)afterConstructedMethod.Invoke(item, [icn, rpcContext, resultObj])!;
                                await tObj;
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }

                    try
                    {
                        var r = (IResult)resultObj;
                        var value = r.GetProperty<IBaseReturnResult>("Value");
                        TT? val = value == null ? default : value.Translate<TT>(); ;
                        var result = new RpcResult<TT>(val, r.Exception)
                        {
                            ElapsedMilliseconds = r.ElapsedMilliseconds,
                        };
                        return result;
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
                else if (_zooyardAttribute.GetType().FullName == typeof(ZooyardGrpcNetAttribute).FullName)
                {
                    var genericMethod = _zooyardPools.GetType().GetMethod(nameof(_zooyardPools.Invoke), 1, [typeof(IInvocation)])!;
                    if (!baseReturnType.IsGenericType)
                    {
                        throw new FrameworkException($"base return type {baseReturnType.FullName} at {typeof(TA).FullName} is not a Generic Type");
                    }

                    var constructedMethod = genericMethod.MakeGenericMethod(baseReturnType.GetGenericArguments());

                    object? resultObj = null;
                    try
                    {
                        var taskObj = (Task)constructedMethod.Invoke(_zooyardPools, [icn])!;

                        await taskObj;

                        resultObj = taskObj.GetProperty<object>("Result");
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    newActivityLOCAL.Value?.Stop();

                    var genericType = resultObj.GetType();

                    //todo after invoke
                    if (_interceptors != null && _interceptors.Count() > 0)
                    {
                        foreach (var item in _interceptors)
                        {
                            try
                            {
                                var afterGenericMethod = item.GetType().GetMethod(nameof(item.AfterCall))!;
                                var afterConstructedMethod = afterGenericMethod.MakeGenericMethod([genericType]);
                                var tObj = (Task)afterConstructedMethod.Invoke(item, [icn, rpcContext, resultObj])!;
                                await tObj;
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }

                    try
                    {
                        var r = (IResult)resultObj;
                        var value = r.GetProperty<IBaseReturnResult>("Value");
                        TT? val = value == null ? default : value.Translate<TT>(); ;
                        var result = new RpcResult<TT>(val, r.Exception)
                        {
                            ElapsedMilliseconds = r.ElapsedMilliseconds,
                        };
                        return result;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                else
                {
                    var result = await ReturnCall<TT>();
                    return result;
                }
            }

            async Task<IResult<TA>?> ReturnCall<TA>()
            {
                // begin invoke
                var resultInner = await _zooyardPools.Invoke<TA>(icn);
                // end invoke

                newActivityLOCAL.Value?.Stop();

                //todo after invoke
                if (_interceptors != null && _interceptors.Count() > 0)
                {
                    foreach (var item in _interceptors)
                    {
                        await item.AfterCall<TA>(icn, rpcContext, resultInner);
                    }
                }
                return resultInner;
            }

            if (result != null)
            {
                returnValue = result.Value;
                context.Packed.ReturnValue = returnValue;
            }
        }
        catch (Exception e)
        {
            var attribute = context.Method.GetCustomAttribute<DefaultValueAttribute>();
            if (attribute == null)
            {
                attribute = context.Packed.DeclaringType.GetCustomAttribute<DefaultValueAttribute>();
            }

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



    private void InjectHeaders(Activity currentActivity, IDictionary<string, string?> headers)
    {
        if (currentActivity.IdFormat == ActivityIdFormat.W3C)
        {
            if (!headers.ContainsKey(TraceParentHeaderName))
            {
                headers.Add(TraceParentHeaderName, currentActivity.Id);
                if (currentActivity.TraceStateString != null)
                {
                    headers.Add(TraceStateHeaderName, currentActivity.TraceStateString);
                }
            }
        }
        else
        {
            if (!headers.ContainsKey(RequestIdHeaderName))
            {
                headers.Add(RequestIdHeaderName, currentActivity.Id);
            }
        }

        // we expect baggage to be empty or contain a few items
        using (IEnumerator<KeyValuePair<string, string?>> e = currentActivity.Baggage.GetEnumerator())
        {
            if (e.MoveNext())
            {
                var baggage = new List<string>();
                do
                {
                    KeyValuePair<string, string?> item = e.Current;
                    baggage.Add(new NameValueHeaderValue(WebUtility.UrlEncode(item.Key), WebUtility.UrlEncode(item.Value)).ToString());
                }
                while (e.MoveNext());
                headers.Add(CorrelationContextHeaderName, string.Join(',', baggage));
            }
        }
    }

    public const string ActivityName = "Zooyard.RequestOut";
    public const string RequestIdHeaderName = "Request-Id";
    public const string CorrelationContextHeaderName = "Correlation-Context";

    public const string TraceParentHeaderName = "traceparent";
    public const string TraceStateHeaderName = "tracestate";

}
