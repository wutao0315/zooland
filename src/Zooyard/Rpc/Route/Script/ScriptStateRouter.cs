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

    public ScriptStateRouter(URL address) : base(address)
    {
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
        catch(Exception e)
        {
            Logger().LogError(e, $"route error, rule has been ignored. rule: {_rule}, url: {RpcContext.GetContext().Url}");
            throw;
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
        var result = ENGINES.GetOrAdd(Constants.DEFAULT_SCRIPT_TYPE_KEY, (t) => {

            // 这边定义一个变量engine  生成一个v8引擎  用来执行js脚本
            var scriptEngine = new V8ScriptEngine(Constants.DEFAULT_SCRIPT_TYPE_KEY, 
                V8ScriptEngineFlags.EnableDebugging | V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart, 9222);
            // 里面的参数9222为调试端口，
            // V8ScriptEngineFlags.EnableDebugging 是否启用调试模式
            // V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart  异步停止或开始等待调试
            // type  调试引擎模式
            scriptEngine.DocumentSettings.AccessFlags = Microsoft.ClearScript.DocumentAccessFlags.EnableFileLoading;
            scriptEngine.DefaultAccess = Microsoft.ClearScript.ScriptAccess.Full; // 这两行是为了允许加载js文件

            return scriptEngine;
        });

        return result;
    }

    protected override IList<URL> DoRoute(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder, Holder<String> messageHolder)
    {
        if (_engine == null || _script == null)
        {
            if (needToPrintMessage)
            {
                messageHolder.Value = "Directly Return. Reason: engine or function is null";
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
            Logger().LogError(e, $"route error, rule has been ignored. rule: {_rule}, method:{invocation.MethodInfo.Name}, url: {RpcContext.GetContext().Url}");
            return invokers;
        }
    }

    public override bool Runtime => this.Address.GetParameter(Constants.RUNTIME_KEY, false);

    public override bool Force => this.Address.GetParameter(Constants.FORCE_KEY, false);

}
