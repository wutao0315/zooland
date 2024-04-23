
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using Zooyard;
using Zooyard.Attributes;
using Zooyard.DynamicProxy;

namespace RpcContractHttp;

public class HelloServiceClientTest : ProxyExecutor, IHelloService
{
    private readonly InterfaceMapping _interfaceMapping;

    //private readonly IDictionary<MethodInfo, int> _methodToToken;

    //private readonly Dictionary<int, MethodInfo> _methodInterface;
    //private readonly Dictionary<MethodInfo, int> _methodToken;
    //private readonly List<MethodInfo> _methodsByToken = new();
    private readonly ZooyardInvoker _invoker;
    private readonly Type _declaringType;
    public HelloServiceClientTest(ILogger<IHelloService> logger, IServiceProvider serviceProvider)
    {
        _declaringType = typeof(IHelloService);
        var zooyardAttr = _declaringType.GetCustomAttribute<ZooyardAttribute>();
        _invoker = new ZooyardInvoker(logger, serviceProvider, zooyardAttr);
        //_methodToToken = _invoker.GetMethodPosition(_declaringType);
        //var methods = _declaringType.GetRuntimeMethods();
        //foreach (var method in methods)
        //{
        //    if (!_methodToToken.TryGetValue(method.ToString(), out _))
        //    {
        //        _methodsByToken.Add(method);
        //        var token = _methodsByToken.Count - 1;
        //        _methodToToken[method.ToString()] = token;
        //    }
        //}

        _interfaceMapping = this.GetType().GetInterfaceMap(_declaringType);

        //_methodInterface = new();
        //_methodToken = new();

        //InterfaceMapping map = this.GetType().GetInterfaceMap(_declaringType);
        //for (int ctr = 0; ctr < map.InterfaceMethods.Length; ctr++)
        //{
        //    MethodInfo im = map.InterfaceMethods[ctr];
        //    MethodInfo tm = map.TargetMethods[ctr];

        //    _methodInterface.Add(ctr, im);
        //    _methodToken.Add(tm, ctr);
        //}
    }

    //internal MethodInfo ResolveMethodToken(int token)
    //{
    //    Debug.Assert(token >= 0 && token < _methodsByToken.Count);
    //    return _methodsByToken[token];
    //}

    //public MethodInfo GetImplementingMethod(MethodInfo interfaceMethod, Type classType)
    //{
    //    #region Parameter Validation

    //    if (Object.ReferenceEquals(null, interfaceMethod))
    //        throw new ArgumentNullException("interfaceMethod");
    //    if (Object.ReferenceEquals(null, classType))
    //        throw new ArgumentNullException("classType");
    //    if (!interfaceMethod.DeclaringType.IsInterface)
    //        throw new ArgumentException("interfaceMethod", "interfaceMethod is not defined by an interface");

    //    #endregion

    //    InterfaceMapping map = classType.GetInterfaceMap(interfaceMethod.DeclaringType);
    //    MethodInfo result = null;

    //    for (var index = 0; index < map.InterfaceMethods.Length; index++)
    //    {
    //        if (map.InterfaceMethods[index] == interfaceMethod)
    //            result = map.TargetMethods[index];
    //    }

    //    Debug.Assert(result != null, "Unable to locate MethodInfo for implementing method");

    //    return result;
    //}

    [ZooyardImpl]
    public void CallName(string name)
    {
        //var stackTrace = new StackTrace(true);
        //var (mi, mtoken) = _invoker.GetInterfaceMethod(stackTrace, _interfaceMapping);

        var method = MethodBase.GetCurrentMethod();
        var (mi,mtoken) = _invoker.GetInterfaceMethodBase(method, _interfaceMapping);

        object[] args = [name];
        var context = _invoker.GetMethodResolverContext(this, _declaringType, mi, mtoken, args);

        _invoker.Invoke(context);
    }

    [ZooyardImpl]
    public async Task<string?> CallNameVoid()
    {
        //var stackTrace = new StackTrace(true);
        //var (mi, mtoken) = _invoker.GetInterfaceMethod(stackTrace, _interfaceMapping);

        var method = MethodBase.GetCurrentMethod();
        var (mi, mtoken) = _invoker.GetInterfaceMethodBase(method, _interfaceMapping);

        object[] args = [];
        var context = _invoker.GetMethodResolverContext(this, _declaringType, mi, mtoken, args);
        var result = await _invoker.InvokeAsync<string>(context);
        return result;
    }

    [ZooyardImpl]
    public async Task CallVoid()
    {
        //var stackTrace = new StackTrace(true);
        //var (mi, mtoken) = _invoker.GetInterfaceMethod(stackTrace, _interfaceMapping);

        var method = MethodBase.GetCurrentMethod();
        var (mi, mtoken) = _invoker.GetInterfaceMethodBase(method, _interfaceMapping);

        object[] args = [];
        var context = _invoker.GetMethodResolverContext(this, _declaringType, mi, mtoken, args);
        await _invoker.InvokeAsync(context);
    }
    [ZooyardImpl]
    public async Task<Result<HelloResult>?> GetPage(string name)
    {
        //var stackTrace = new StackTrace(true);
        //var (mi, mtoken) = _invoker.GetInterfaceMethod(stackTrace, _interfaceMapping);

        var method = MethodBase.GetCurrentMethod();
        var (mi, mtoken) = _invoker.GetInterfaceMethodBase(method, _interfaceMapping);

        object[] args = [name];
        var context = _invoker.GetMethodResolverContext(this, _declaringType, mi, mtoken, args);
        var result = await _invoker.InvokeAsync<Result<HelloResult>>(context);
        return result;
    }

    [ZooyardImpl]
    public async Task<string?> Hello(string name)
    {
        //var stackTrace = new StackTrace(true);
        //var (mi, mtoken) = _invoker.GetInterfaceMethod(stackTrace, _interfaceMapping);

        var method = MethodBase.GetCurrentMethod();
        var (mi, mtoken) = _invoker.GetInterfaceMethodBase(method, _interfaceMapping);

        object[] args = [name];
        var context = _invoker.GetMethodResolverContext(this, _declaringType, mi, mtoken, args);
        var result = await _invoker.InvokeAsync<string>(context);
        return result;
    }

    [ZooyardImpl]
    public async Task<HelloResult?> SayHello(string name)
    {
        //var stackTrace = new StackTrace(true);
        //var (mi, mtoken) = _invoker.GetInterfaceMethod(stackTrace, _interfaceMapping);

        var method = MethodBase.GetCurrentMethod();
        var (mi, mtoken) = _invoker.GetInterfaceMethodBase(method, _interfaceMapping);

        object[] args = [name];
        var context = _invoker.GetMethodResolverContext(this, _declaringType, mi, mtoken, args);
        var result = await _invoker.InvokeAsync<HelloResult>(context);
        return result;
    }

    [ZooyardImpl]
    public async Task<string?> ShowHello(HelloResult name)
    {
        //var stackTrace = new StackTrace(true);
        //var (mi, mtoken) = _invoker.GetInterfaceMethod(stackTrace, _interfaceMapping);

        var method = MethodBase.GetCurrentMethod();
        var (mi, mtoken) = _invoker.GetInterfaceMethodBase(method, _interfaceMapping);

        object[] args = [name];
        var context = _invoker.GetMethodResolverContext(this, _declaringType, mi, mtoken, args);
        var result = await _invoker.InvokeAsync<string>(context);
        return result;
    }
}
