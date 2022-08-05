using Zooyard.Logging;
using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Script;

public class ScriptStateRouter<T> : AbstractStateRouter<T>
{
    public const string NAME = "SCRIPT_ROUTER";
    private const int SCRIPT_ROUTER_DEFAULT_PRIORITY = 0;
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ScriptStateRouter<>));

    //private static readonly Dictionary<String, ScriptEngine> ENGINES = new ConcurrentDictionary<>();

    //private readonly ScriptEngine engine;

    private readonly string rule;

    //private CompiledScript function;

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

        //engine = getEngine(url);
        //rule = getRule(url);
        //try
        //{
        //    Compilable compilable = (Compilable)engine;
        //    function = compilable.compile(rule);
        //}
        //catch //(ScriptException e)
        //{
        //    //Logger().LogError("route error, rule has been ignored. rule: " + rule +
        //    //    ", url: " + RpcContext.getServiceContext().getUrl(), e);
        //}
    }

    /// <summary>
    /// get rule from url parameters.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    /// <exception cref="IllegalStateException"></exception>
    private String getRule(URL url)
    {
        string vRule = url.GetParameterAndDecoded(Constants.RULE_KEY);
        if (string.IsNullOrWhiteSpace(vRule))
        {
            throw new Exception("route rule can not be empty.");
        }
        return vRule;
    }

    ///// <summary>
    ///// create ScriptEngine instance by type from url parameters, then cache it
    ///// </summary>
    ///// <param name="url"></param>
    ///// <returns></returns>
    ///// <exception cref="IllegalStateException"></exception>
    //private ScriptEngine getEngine(URL url)
    //{
    //    string type = url.GetParameter(Constants.TYPE_KEY, Constants.DEFAULT_SCRIPT_TYPE_KEY);

    //    return ENGINES.computeIfAbsent(type, t => {
    //        ScriptEngine scriptEngine = new ScriptEngineManager().getEngineByName(type);
    //        if (scriptEngine == null)
    //        {
    //            throw new Exception("unsupported route engine type: " + type);
    //        }
    //        return scriptEngine;
    //    });
    //}

    protected override BitList<IInvoker> DoRoute(BitList<IInvoker> invokers, URL url, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode<T>> nodeHolder, Holder<String> messageHolder)
    {
        //if (engine == null || function == null)
        //{
        //    if (needToPrintMessage)
        //    {
        //        messageHolder.Value = "Directly Return. Reason: engine or function is null";
        //    }
        //    return invokers;
        //}
        //Bindings bindings = createBindings(invokers, invocation);
        //return getRoutedInvokers(invokers, AccessController.doPrivileged((PrivilegedAction<Object>)()-> {
        //    try
        //    {
        //        return function.eval(bindings);
        //    }
        //    catch (ScriptException e)
        //    {
        //        logger.error("route error, rule has been ignored. rule: " + rule + ", method:" +
        //            invocation.getMethodName() + ", url: " + RpcContext.getContext().getUrl(), e);
        //        return invokers;
        //    }
        //}, accessControlContext));
        return null;
    }

    /// <summary>
    /// get routed invokers from result of script rule evaluation
    /// </summary>
    /// <param name="invokers"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected BitList<IInvoker> getRoutedInvokers(BitList<IInvoker> invokers, object obj)
    {
        BitList<IInvoker> result = invokers;//.clone();
                                            //    if (obj is IInvoker[]) {
                                            //    result.retainAll(Arrays.asList((IInvoker[])obj));
                                            //} else if (obj is object[]) {
                                            //    result.retainAll(Arrays.stream((Object[])obj).map(item-> (Invoker<T>) item).collect(Collectors.toList()));
                                            //} else
                                            //{
                                            //    result.retainAll((List<Invoker<T>>)obj);
                                            //}
        return result;
    }

    ///**
    // * create bindings for script engine
    // */
    //private Bindings createBindings(List<IInvoker> invokers, IInvocation invocation)
    //{
    //    Bindings bindings = engine.createBindings();
    //    // create a new List of invokers
    //    bindings.put("invokers", new List<IInvoker>(invokers));
    //    bindings.put("invocation", invocation);
    //    bindings.put("context", RpcContext.GetContext().Attachments);
    //    return bindings;
    //}

    public bool Runtime => this.Url.GetParameter(Constants.RUNTIME_KEY, false);

    public bool Force => this.Url.GetParameter(Constants.FORCE_KEY, false);

}
