using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Zooyard.Management;

//using Zooyard.Logging;
using Zooyard.Rpc.Route.Condition.Config.Model;
using Zooyard.Rpc.Route.State;
using Zooyard.Rpc.Route.Tag.Model;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Condition.Config;

public abstract class ListenableStateRouter : AbstractStateRouter
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public const string NAME = "LISTENABLE_ROUTER";
    private const string RULE_SUFFIX = ".condition-router";
    private volatile ConditionRouterRule? routerRule;
    private volatile List<ConditionStateRouter> conditionRouters = new();
    private string ruleKey;

    //private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    //public ListenableStateRouter(ILoggerFactory loggerFactory, IOptionsMonitor<ZooyardOption> zooyard, URL address, string ruleKey):base(address)
    private readonly IRpcStateLookup _stateLookup;
    public ListenableStateRouter(ILoggerFactory loggerFactory, IRpcStateLookup stateLookup, URL address, string ruleKey) : base(address)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ListenableStateRouter>();
        _stateLookup = stateLookup;
        _stateLookup.OnChange(OnChanged);

        this.Force = false;
        this.Init(ruleKey);
        this.ruleKey = ruleKey;
    }
    // 监听配置或者服务注册变化，清空缓存
    void OnChanged(IRpcStateLookup value)
    {
        var applicationName = Environment.GetEnvironmentVariable("applicationName") ?? "system_name";
        if (!value.GetServices().TryGetValue(applicationName, out var serviceOption))
        {
            return;
        }

        serviceOption.Model.Config.Metadata.TryGetValue("route.rule", out var ruleContent);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"Notification of tag rule, change type is: {value}, raw rule is:\n {ruleContent}");
        }


        if (string.IsNullOrWhiteSpace(ruleContent))
        {
            routerRule = null;
            conditionRouters = new();
        }
        else
        {
            try
            {
                routerRule = ConditionRuleParser.Parse(ruleContent);
                GenerateConditions(routerRule);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to parse the raw condition rule and it will not take effect, please check if the condition rule matches with the template, the raw rule is:\n {ruleContent}");
            }
        }
    }
    //@Override
    //public synchronized void process(ConfigChangedEvent event) {
    //    if (logger.isDebugEnabled()) {
    //        logger.debug("Notification of condition rule, change type is: " + event.getChangeType() +
    //                ", raw rule is:\n " + event.getContent());
    //    }

    //    if (event.getChangeType().equals(ConfigChangeType.DELETED)) {
    //        routerRule = null;
    //        conditionRouters = Collections.emptyList();
    //    } else {
    //        try {
    //            routerRule = ConditionRuleParser.parse(event.getContent());
    //            generateConditions(routerRule);
    //        } catch (Exception e) {
    //            logger.error("Failed to parse the raw condition rule and it will not take effect, please check " +
    //                    "if the condition rule matches with the template, the raw rule is:\n " + event.getContent(), e);
    //        }
    //    }
    //}

    protected override IList<URL> DoRoute(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder, Holder<String> messageHolder)
    {
        if (invokers.Count == 0 || conditionRouters.Count == 0)
        {
            if (needToPrintMessage)
            {
               messageHolder.Value = "Directly return. Reason: Invokers from previous router is empty or conditionRouters is empty.";
            }
            return invokers;
        }

        // We will check enabled status inside each router.
        StringBuilder? resultMessage = null;
        if (needToPrintMessage)
        {
            resultMessage = new StringBuilder();
        }
        foreach (AbstractStateRouter router in conditionRouters)
        {
            invokers = router.Route(invokers, address, invocation, needToPrintMessage, nodeHolder);
            if (needToPrintMessage)
            {
                resultMessage!.Append(messageHolder.Value);
            }
        }

        if (needToPrintMessage)
        {
            messageHolder.Value = resultMessage!.ToString();
        }

        return invokers;
    }

    public override bool Force { get => (routerRule != null && routerRule.Force); set => base.Force = value; }

    private bool RuleRuntime=> routerRule != null && routerRule.Valid && routerRule.Runtime;

    private void GenerateConditions(ConditionRouterRule? rule)
    {
        if (rule != null && rule.Valid)
        {
            this.conditionRouters = (from a in rule.Conditions
                                     select new ConditionStateRouter(_loggerFactory, Address, a, rule.Force, rule.Enabled)).ToList();

            foreach (var conditionRouter in this.conditionRouters)
            {
                conditionRouter.NextRouter = TailStateRouter.GetInstance();
            }
        }
    }

    private void Init(string? ruleKey)
    {
        if (string.IsNullOrWhiteSpace(ruleKey))
        {
            return;
        }
        string routerKey = ruleKey + RULE_SUFFIX;
        //this.getRuleRepository().addListener(routerKey, this);
        //string rule = this.getRuleRepository().getRule(routerKey, DynamicConfiguration.DEFAULT_GROUP);
        //if (StringUtils.isNotEmpty(rule))
        //{
        //    this.process(new ConfigChangedEvent(routerKey, DynamicConfiguration.DEFAULT_GROUP, rule));
        //}
    }

    //public void Stop()
    //{
    //    //this.getRuleRepository().removeListener(ruleKey + RULE_SUFFIX, this);
    //}
}
