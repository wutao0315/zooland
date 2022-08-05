using System.Text;
using Zooyard.Logging;
using Zooyard.Rpc.Route.Condition.Config.Model;
using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Condition.Config;

public abstract class ListenableStateRouter<T> : AbstractStateRouter<T>
{
    public const string NAME = "LISTENABLE_ROUTER";
    private const string RULE_SUFFIX = ".condition-router";
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ListenableStateRouter<>));
    private volatile ConditionRouterRule routerRule;
    private volatile List<ConditionStateRouter<T>> conditionRouters = new();
    private string ruleKey;

    public ListenableStateRouter(URL url, string ruleKey):base(url)
    {
        this.Force = false;
        this.init(ruleKey);
        this.ruleKey = ruleKey;
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

    protected override BitList<IInvoker> DoRoute(BitList<IInvoker> invokers, URL url, IInvocation invocation,
                                       bool needToPrintMessage, Holder<RouterSnapshotNode<T>> nodeHolder, Holder<String> messageHolder)
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
        foreach (AbstractStateRouter<T> router in conditionRouters)
        {
            invokers = router.Route(invokers, url, invocation, needToPrintMessage, nodeHolder);
            if (needToPrintMessage)
            {
                resultMessage.Append(messageHolder.Value);
            }
        }

        if (needToPrintMessage)
        {
            messageHolder.Value = resultMessage.ToString();
        }

        return invokers;
    }

    public override bool Force { get => (routerRule != null && routerRule.Force); set => base.Force = value; }

    private bool RuleRuntime=> routerRule != null && routerRule.Valid && routerRule.Runtime;

    //private void generateConditions(ConditionRouterRule rule) {
    //    if (rule != null && rule.isValid()) {
    //        this.conditionRouters = rule.getConditions()
    //                .stream()
    //                .map(condition -> new ConditionStateRouter<T>(getUrl(), condition, rule.isForce(), rule.isEnabled()))
    //                .collect(Collectors.toList());
    //        for (ConditionStateRouter<T> conditionRouter : this.conditionRouters) {
    //            conditionRouter.setNextRouter(TailStateRouter.getInstance());
    //        }
    //    }
    //}

    private void init(string? ruleKey)
    {
        if (string.IsNullOrWhiteSpace(ruleKey))
        {
            return;
        }
        string routerKey = ruleKey + RULE_SUFFIX;
        //this.getRuleRepository().addListener(routerKey, this);
        //String rule = this.getRuleRepository().getRule(routerKey, DynamicConfiguration.DEFAULT_GROUP);
        //if (StringUtils.isNotEmpty(rule))
        //{
        //    this.process(new ConfigChangedEvent(routerKey, DynamicConfiguration.DEFAULT_GROUP, rule));
        //}
    }

    public void Stop()
    {
        //this.getRuleRepository().removeListener(ruleKey + RULE_SUFFIX, this);
    }

}
