using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooyard.Logging;
using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Script;

public class ScriptStateRouter : AbstractStateRouter
{
    public const string NAME = "SCRIPT_ROUTER";
    private const int SCRIPT_ROUTER_DEFAULT_PRIORITY = 0;
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ScriptStateRouter));

    private static readonly ConcurrentDictionary<string, V8ScriptEngine> ENGINES = new ();

    private readonly V8ScriptEngine _engine;

    private readonly string _rule;

    private readonly V8Script _script;

    //private AccessControlContext accessControlContext;
    //{
    //    //Just give permission of reflect to access member.
    //    Permissions perms = new Permissions();
    //    perms.add(new RuntimePermission("accessDeclaredMembers"));
    //    // Cast to Certificate[] required because of ambiguity:
    //    ProtectionDomain domain = new ProtectionDomain(new CodeSource(null, (Certificate[])null), perms);
    //    accessControlContext = new AccessControlContext(new ProtectionDomain[]{domain
    //});
    //}

    public ScriptStateRouter(URL address) : base(address)
    {
        //this.Url = url;

        _engine = GetEngine(address);
        _rule = GetRule(address);
        try
        {
            _engine.AddHostObject("lib", new HostTypeCollection("mscorlib", "System.Core"));
            _script = _engine.Compile(_rule);
            _engine.Execute(_script);
            //(function route(invokers, address, invocation, context) {
            //    List = lib.System.Collections.Generic.List;
            //    var result = new List(lib.System.String);
            //    for (i = 0; i < invokers.Length; i++)
            //    {
            //        if ("10.20.153.10".equals(invokers[i].Host))
            //        {
            //            result.Add(invokers[i]);
            //        }
            //    }
            //    return result;
            //} (invokers));
        }
        catch //(ScriptException e)
        {
            //Logger().LogError("route error, rule has been ignored. rule: " + _rule +
            //    ", url: " + RpcContext.GetServiceContext().Url, e);
        }
    }

    /// <summary>
    /// get rule from url parameters.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private string GetRule(URL address)
    {
        string vRule = address.GetParameterAndDecoded(Constants.RULE_KEY);
        if (string.IsNullOrWhiteSpace(vRule))
        {
            throw new Exception("route rule can not be empty.");
        }
        return vRule;
    }

    /// <summary>
    /// create ScriptEngine instance by type from url parameters, then cache it
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private V8ScriptEngine GetEngine(URL address)
    {
        //string type = address.GetParameter(Constants.TYPE_KEY, Constants.DEFAULT_SCRIPT_TYPE_KEY);

        var result = ENGINES.GetOrAdd(Constants.DEFAULT_SCRIPT_TYPE_KEY, (t) => {

            // 这边定义一个变量engine  生成一个v8引擎  用来执行js脚本
            var scriptEngine = new V8ScriptEngine(Constants.DEFAULT_SCRIPT_TYPE_KEY, V8ScriptEngineFlags.EnableDebugging | V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart, 9222);
            // 里面的参数9222为调试端口， V8ScriptEngineFlags.EnableDebugging 是否启用调试模式
            // V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart  异步停止或开始等待调试
            // type  调试引擎模式
            scriptEngine.DocumentSettings.AccessFlags = Microsoft.ClearScript.DocumentAccessFlags.EnableFileLoading;
            scriptEngine.DefaultAccess = Microsoft.ClearScript.ScriptAccess.Full; // 这两行是为了允许加载js文件

            return scriptEngine;
        });

        return result;
    }

    protected override IList<URL> DoRoute(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage)
    {
        if (_engine == null || _script == null)
        {
            if (needToPrintMessage)
            {
                Logger().LogInformation("Directly Return. Reason: engine or function is null");
                //messageHolder.Value = "Directly Return. Reason: engine or function is null";
            }
            return invokers;
        }

        try
        {
            var result = _engine.Script.route(invokers, address, invocation, RpcContext.GetContext().Attachments);
            return result;
        }
        catch (Exception e)
        {
            Logger().LogError(e, "route error, rule has been ignored. rule: " + _rule + ", method:" +
                invocation.MethodInfo.Name + ", url: " + RpcContext.GetContext().Url);
            return invokers;
        }
        
        //_engine.Invoke();

        //Bindings bindings = createBindings(invokers, invocation);
        //return getRoutedInvokers(invokers, AccessController.doPrivileged((PrivilegedAction<Object>)()-> {
        //    try
        //    {
        //        return _function.eval(bindings);
        //    }
        //    catch(Exception e) //(ScriptException e)
        //    {
        //        Logger().LogError(e, "route error, rule has been ignored. rule: " + _rule + ", method:" +
        //            invocation.MethodInfo.Name + ", url: " + RpcContext.GetContext().Url);
        //        return invokers;
        //    }
        //}, accessControlContext));
        //return null;
    }

    ///// <summary>
    ///// get routed invokers from result of script rule evaluation
    ///// </summary>
    ///// <param name="invokers"></param>
    ///// <param name="obj"></param>
    ///// <returns></returns>
    //protected List<URL> getRoutedInvokers(List<URL> invokers, object obj)
    //{
    //    var result = new List<URL>(invokers);//.clone();
    //    if (obj is URL[] urlList)
    //    {
    //        result.AddRange(urlList);
    //        //result.retainAll(Arrays.asList((IInvoker[])obj));
    //    }
    //    else if (obj is object[] objList)
    //    {
    //        foreach (var item in objList)
    //        {
    //            if (item is URL u) 
    //            {
    //                result.Add(u);
    //            }
    //        }
    //        //result.retainAll(Arrays.stream((Object[])obj).map(item-> (Invoker<T>) item).collect(Collectors.toList()));
    //    }
    //    else
    //    {
    //        result.AddRange((List<URL>)obj);
    //        //result.retainAll((List<Invoker<T>>)obj);
    //    }
    //    return result;
    //}

    ///// <summary>
    ///// create bindings for script engine
    ///// </summary>
    ///// <param name="invokers"></param>
    ///// <param name="invocation"></param>
    ///// <returns></returns>
    //private Bindings createBindings(List<IInvoker> invokers, IInvocation invocation)
    //{
    //    Bindings bindings = engine.createBindings();
    //    // create a new List of invokers
    //    bindings.put("invokers", new List<IInvoker>(invokers));
    //    bindings.put("invocation", invocation);
    //    bindings.put("context", RpcContext.GetContext().Attachments);
    //    return bindings;
    //}

    public override bool Runtime => this.Address.GetParameter(Constants.RUNTIME_KEY, false);

    public override bool Force => this.Address.GetParameter(Constants.FORCE_KEY, false);

}
