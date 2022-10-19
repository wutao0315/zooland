using Microsoft.ClearScript.V8;
using System.Collections.Concurrent;
using Zooyard.Logging;
using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Script;

public class ScriptStateRouter<T> : AbstractStateRouter<T>
{
    public const string NAME = "SCRIPT_ROUTER";
    private const int SCRIPT_ROUTER_DEFAULT_PRIORITY = 0;
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ScriptStateRouter<>));

    private static readonly ConcurrentDictionary<string, V8ScriptEngine> ENGINES = new ();

    private readonly V8ScriptEngine _engine;

    private readonly string _rule;

    private readonly V8Script _function;

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

    public ScriptStateRouter(URL url) : base(url)
    {
        this.Url = url;

        _engine = GetEngine(url);
        _rule = GetRule(url);
        try
        {
            _function = _engine.Compile(_rule);
            //Compilable compilable = (Compilable)_engine;
            //function = compilable.compile(rule);
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
    /// <exception cref="IllegalStateException"></exception>
    private string GetRule(URL url)
    {
        string vRule = url.GetParameterAndDecoded(Constants.RULE_KEY);
        if (string.IsNullOrWhiteSpace(vRule))
        {
            throw new Exception("route rule can not be empty.");
        }
        return vRule;
    }

    /// <summary>
    /// create ScriptEngine instance by type from url parameters, then cache it
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private V8ScriptEngine GetEngine(URL url)
    {
        string type = url.GetParameter(Constants.TYPE_KEY, Constants.DEFAULT_SCRIPT_TYPE_KEY);

        var result = ENGINES.GetOrAdd(type, (t) => {

            // 这边定义一个变量engine  生成一个v8引擎  用来执行js脚本
            V8ScriptEngine scriptEngine = new V8ScriptEngine(type, V8ScriptEngineFlags.EnableDebugging | V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart, 9222);
            // 里面的参数9222为调试端口， V8ScriptEngineFlags.EnableDebugging 是否启用调试模式
            // V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart  异步停止或开始等待调试
            // type  调试引擎模式
            scriptEngine.DocumentSettings.AccessFlags = Microsoft.ClearScript.DocumentAccessFlags.EnableFileLoading;
            scriptEngine.DefaultAccess = Microsoft.ClearScript.ScriptAccess.Full; // 这两行是为了允许加载js文件

            return scriptEngine;
        });

        return result;

        //return ENGINES.computeIfAbsent(type, t =>
        //{
        //    ScriptEngine scriptEngine = new ScriptEngineManager().getEngineByName(type);
        //    if (scriptEngine == null)
        //    {
        //        throw new Exception("unsupported route engine type: " + type);
        //    }
        //    return scriptEngine;
        //});
    }

    protected override BitList<IInvoker> DoRoute(BitList<IInvoker> invokers, URL url, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode<T>> nodeHolder, Holder<String> messageHolder)
    {
        if (_engine == null || _function == null)
        {
            if (needToPrintMessage)
            {
                messageHolder.Value = "Directly Return. Reason: engine or function is null";
            }
            return invokers;
        }


        _engine.Execute(_function);

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
        return null;
    }

    ///// <summary>
    ///// get routed invokers from result of script rule evaluation
    ///// </summary>
    ///// <param name="invokers"></param>
    ///// <param name="obj"></param>
    ///// <returns></returns>
    //protected BitList<IInvoker> getRoutedInvokers(BitList<IInvoker> invokers, object obj)
    //{
    //    BitList<IInvoker> result = invokers;//.clone();
    //                                        //    if (obj is IInvoker[]) {
    //                                        //    result.retainAll(Arrays.asList((IInvoker[])obj));
    //                                        //} else if (obj is object[]) {
    //                                        //    result.retainAll(Arrays.stream((Object[])obj).map(item-> (Invoker<T>) item).collect(Collectors.toList()));
    //                                        //} else
    //                                        //{
    //                                        //    result.retainAll((List<Invoker<T>>)obj);
    //                                        //}
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

    public override bool Runtime => this.Url.GetParameter(Constants.RUNTIME_KEY, false);

    public override bool Force => this.Url.GetParameter(Constants.FORCE_KEY, false);

}
