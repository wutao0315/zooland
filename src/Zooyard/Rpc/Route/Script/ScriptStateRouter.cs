using Jint;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Script;

public class ScriptStateRouter : AbstractStateRouter
{

    public const string NAME = "SCRIPT_ROUTER";
    //private const int SCRIPT_ROUTER_DEFAULT_PRIORITY = 0;
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ScriptStateRouter));
    private readonly ILogger _logger;

    private static readonly ConcurrentDictionary<string, Engine> ENGINES = new ();

    private readonly Engine _engine;

    private readonly string _rule;

    private CancellationTokenSource _cts = new();

    public ScriptStateRouter(ILoggerFactory loggerFactory, URL address) : base(address)
    {
        _logger = loggerFactory.CreateLogger<ScriptStateRouter>();
        _engine = GetEngine(address);
        _rule = GetRule(address);

        try
        {
            _engine.Execute(_rule);
            //function route(invokers, address, invocation, context) {
            //    var Zooyard = importNamespace('Zooyard')
            //    var ListOfURL = System.Collections.Generic.List(Zooyard.URL);
            //    var list = new ListOfURL();
            //    for (i = 0; i < invokers.Length; i++)
            //    {
            //        if ("10.20.153.10".equals(invokers[i].Host))
            //        {
            //            list.Add(invokers[i]);
            //        }
            //    }
            //    return list;
            //}

        }
        catch (Exception e)
        {
            _logger.LogError(e, $"route error, rule has been ignored. rule: {_rule}, url: {RpcContext.GetContext().Url}");
            throw;
        }
    }

    /// <summary>
    /// get rule from url parameters.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private string GetRule(URL address)
    {
        string vRule = address.GetParameterAndDecoded(Constants.RULE_KEY);
        if (string.IsNullOrWhiteSpace(vRule))
        {
            vRule = @"function route(invokers, address, invocation, context)
            {
                return invokers;
            }";
            //throw new Exception("route rule can not be empty.");
        }
        return vRule;
    }

    /// <summary>
    /// create ScriptEngine instance by type from url parameters, then cache it
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private Engine GetEngine(URL address)
    {
        var result = ENGINES.GetOrAdd(Constants.DEFAULT_SCRIPT_TYPE_KEY, (t) => {

            // 这边定义一个变量engine  生成一个v8引擎  用来执行js脚本
            var scriptEngine = new Engine(options => {

                options.AllowClr();

                options.AllowClr(typeof(URL).Assembly);

                var limitMemory = address.GetParameter("limit_memory", 4_000_000);
                // Limit memory allocations to 4 MB
                options.LimitMemory(limitMemory);

                var timeout_interval = address.GetParameter("timeout_interval", 4);
                // Set a timeout to 4 seconds.
                options.TimeoutInterval(TimeSpan.FromSeconds(timeout_interval));

                var max_statements = address.GetParameter("max_statements", 1000);
                // Set limit of 1000 executed statements.
                options.MaxStatements(1000);

                // Use a cancellation token.
                options.CancellationToken(_cts.Token);
                });

            return scriptEngine;
        });

        return result;
    }

    protected override IList<URL> DoRoute(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder, Holder<string> messageHolder)
    {
        if (_engine == null || string.IsNullOrWhiteSpace(_rule))
        {
            if (needToPrintMessage)
            {
                messageHolder.Value = "Directly Return. Reason: engine or function is null";
            }
            return invokers;
        }

        try
        {
            var result = _engine.Invoke("route", invokers, address, invocation, RpcContext.GetContext().Attachments);
            var listObj = result.ToObject();
            var listResult = listObj as List<URL>;
            return listResult ?? new ();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"route error, rule has been ignored. rule: {_rule}, method:{invocation.MethodInfo.Name}, url: {RpcContext.GetContext().Url}");
            return invokers;
        }
    }

    public override bool Runtime => this.Address.GetParameter(Constants.RUNTIME_KEY, false);

    public override bool Force => this.Address.GetParameter(Constants.FORCE_KEY, false);

    public override void Dispose()
    {
        base.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }
}
