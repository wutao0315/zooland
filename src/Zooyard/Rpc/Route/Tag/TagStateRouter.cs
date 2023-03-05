using System.Linq;
using Zooyard.Logging;
using Zooyard.Rpc.Route.State;
using Zooyard.Rpc.Route.Tag.Model;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Tag;

public class TagStateRouter : AbstractStateRouter
{
    public const string NAME = "TAG_ROUTER";
    private const string TAG_KEY = "tag";
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(TagStateRouter));
    //private static readonly string RULE_SUFFIX = ".tag-router";

    private TagRouterRule? tagRouterRule;
    private string? application;

    public TagStateRouter(URL address) : base(address)
    {
    }

    //public void process(ConfigChangedEvent @event)
    //{
    //    if (Logger().IsEnabled(LogLevel.Debug))
    //    {
    //        Logger().LogDebug("Notification of tag rule, change type is: " + @event.getChangeType() + ", raw rule is:\n " + @event.getContent());
    //    }

    //    try
    //    {
    //        if (@event.getChangeType().equals(ConfigChangeType.DELETED))
    //        {
    //            this.tagRouterRule = null;
    //        }
    //        else
    //        {
    //            this.tagRouterRule = TagRuleParser.parse(@event.getContent());
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Logger().LogError("Failed to parse the raw tag router rule and it will not take effect, please check if the " + "rule matches with the template, the raw rule is:\n ", e);
    //    }
    //}

    protected override IList<URL> DoRoute(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage)
    {
        if (invokers.Count == 0)
        {
            if (needToPrintMessage)
            {
                Logger().LogInformation("Directly Return. Reason: Invokers from previous router is empty.");
                //messageHolder.Value = "Directly Return. Reason: Invokers from previous router is empty.";
            }
            return invokers;
        }

        // since the rule can be changed by config center, we should copy one to use.
        TagRouterRule? tagRouterRuleCopy = tagRouterRule;
        if (tagRouterRuleCopy == null || !tagRouterRuleCopy.Valid || !tagRouterRuleCopy.Enabled)
        {
            if (needToPrintMessage)
            {
                Logger().LogInformation("Disable Tag Router. Reason: tagRouterRule is invalid or disabled");
               // messageHolder.Value = "Disable Tag Router. Reason: tagRouterRule is invalid or disabled";
            }
            return filterUsingStaticTag(invokers, address, invocation);
        }

        IList<URL> result = invokers;
        string tag = string.IsNullOrWhiteSpace(invocation.GetAttachment(TAG_KEY)) ? address.GetParameter(TAG_KEY)! : invocation.GetAttachment(TAG_KEY);

        // if we are requesting for a Provider with a specific tag
        if (!string.IsNullOrWhiteSpace(tag))
        {
            tagRouterRuleCopy.getTagnameToAddresses().TryGetValue(tag, out List<string>? addresses);
            // filter by dynamic tag group first
            if (addresses!= null && addresses.Count > 0)
            {
                result = filterInvoker(invokers, invoker=>addressMatches(invoker, addresses));
                // if result is not null OR it's null but force=true, return result directly
                if (result.Count > 0 || tagRouterRuleCopy.Force)
                {
                    if (needToPrintMessage)
                    {
                        Logger().LogInformation("Use tag " + tag + " to route. Reason: result is not null OR it's null but force=true");
                        // messageHolder.Value = "Use tag " + tag + " to route. Reason: result is not null OR it's null but force=true";
                    }
                    return result;
                }
            }
            else
            {
                // dynamic tag group doesn't have any item about the requested app OR it's null after filtered by
                // dynamic tag group but force=false. check static tag
                result = filterInvoker(invokers, invoker=>tag.Equals(invoker.GetParameter(TAG_KEY)));
            }
            // If there's no tagged providers that can match the current tagged request. force.tag is set by default
            // to false, which means it will invoke any providers without a tag unless it's explicitly disallowed.
            if (result.Count > 0 || isForceUseTag(invocation))
            {
                if (needToPrintMessage)
                {
                    Logger().LogInformation("Use tag " + tag + " to route. Reason: result is not empty or ForceUseTag key is true in invocation");
                    // messageHolder.Value = "Use tag " + tag + " to route. Reason: result is not empty or ForceUseTag key is true in invocation";
                }
                return result;
            }
            // FAILOVER: return all Providers without any tags.
            else
            {
                var tmp = filterInvoker(invokers, invoker=>addressNotMatches(invoker,
                    tagRouterRuleCopy.Addresses));
                if (needToPrintMessage)
                {
                    Logger().LogInformation("FAILOVER: return all Providers without any tags");
                    //messageHolder.Value = "FAILOVER: return all Providers without any tags";
                }
                return filterInvoker(tmp, invoker=>string.IsNullOrWhiteSpace(invoker.GetParameter(TAG_KEY)));
            }
        }
        else
        {
            // List<String> addresses = tagRouterRule.filter(providerApp);
            // return all addresses in dynamic tag group.
            List<string> addresses = tagRouterRuleCopy.Addresses;
            if (addresses?.Count > 0)
            {
                result = filterInvoker(invokers, invoker=>addressNotMatches(invoker, addresses));
                // 1. all addresses are in dynamic tag group, return empty list.
                if (result.Count == 0)
                {
                    if (needToPrintMessage)
                    {
                        Logger().LogInformation("all addresses are in dynamic tag group, return empty list");
                        //messageHolder.Value = "all addresses are in dynamic tag group, return empty list";
                    }
                    return result;
                }
                // 2. if there are some addresses that are not in any dynamic tag group, continue to filter using the
                // static tag group.
            }
            if (needToPrintMessage)
            {
                Logger().LogInformation("filter using the static tag group");
                //messageHolder.Value = "filter using the static tag group";
            }
            return filterInvoker(result, invoker=> 
            {
                string? localTag = invoker.GetParameter(TAG_KEY);
                return string.IsNullOrWhiteSpace(localTag) || !tagRouterRuleCopy.getTagNames().Contains(localTag);
            });
        }
    }


    /// <summary>
    /// 
    /// If there's no dynamic tag rule being set, use static tag in URL.
    /// <p>
    /// A typical scenario is a Consumer using version 2.7.x calls Providers using version 2.6.x or lower,
    /// the Consumer should always respect the tag in provider URL regardless of whether a dynamic tag rule has been set to it or not.
    /// <p>
    /// TODO, to guarantee consistent behavior of interoperability between 2.6- and 2.7+, this method should has the same logic with the TagRouter in 2.6.x.
    /// 
    /// </summary>
    /// <param name="invokers"></param>
    /// <param name="url"></param>
    /// <param name="invocation"></param>
    /// <returns></returns>
    private IList<URL> filterUsingStaticTag(IList<URL> invokers, URL url, IInvocation invocation)
    {
        IList<URL>? result = null;
        // Dynamic param
        string tag = string.IsNullOrWhiteSpace(invocation.GetAttachment(TAG_KEY)) ? url.GetParameter(TAG_KEY)! :invocation.GetAttachment(TAG_KEY);
        // Tag request
        if (!string.IsNullOrWhiteSpace(tag))
        {
            result = filterInvoker(invokers, invoker => tag.Equals(invoker.GetParameter(TAG_KEY)));
            if (result.Count == 0 && !isForceUseTag(invocation))
            {
                result = filterInvoker(invokers, invoker=>string.IsNullOrWhiteSpace(invoker.GetParameter(TAG_KEY)));
            }
        }
        else
        {
            result = filterInvoker(invokers, invoker => string.IsNullOrWhiteSpace(invoker.GetParameter(TAG_KEY)));
        }
        return result;
    }

    public override bool Runtime => tagRouterRule != null && tagRouterRule.Runtime;
    public override bool Force => tagRouterRule != null && tagRouterRule.Force;

    private bool isForceUseTag(IInvocation invocation)
    {
        return bool.Parse(invocation.GetAttachment(Constants.FORCE_USE_TAG, this.Address.GetParameter(Constants.FORCE_USE_TAG, "false")));
    }

    private IList<URL> filterInvoker(IList<URL> invokers, Func<URL, bool> predicate)
    {
        var result = invokers.Where(predicate).ToList();
        return result;
        //if (invokers.stream().allMatch(predicate))
        //{
        //    return invokers;
        //}

        //IList<URL> newInvokers = invokers;//.clone();
        //newInvokers.removeIf(invoker-> !predicate.test(invoker));

        //return newInvokers;
    }

    private bool addressMatches(URL url, List<String> addresses)
    {
        return addresses != null && checkAddressMatch(addresses, url.Host, url.Port);
    }

    private bool addressNotMatches(URL url, List<String> addresses)
    {
        return addresses == null || !checkAddressMatch(addresses, url.Host, url.Port);
    }

    private bool checkAddressMatch(List<String> addresses, String host, int port)
    {
        foreach (var address in addresses)
        {
            try
            {
                if (NetUtil.matchIpExpression(address, host, port))
                {
                    return true;
                }
                if ((NetUtil.ANYHOST + ":" + port).Equals(address))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger().LogError(e, "The format of ip address is invalid in tag route. Address :" + address);
            }
        }
        return false;
    }

    public void setApplication(string app)
    {
        this.application = app;
    }

    //public void Notify(BitList<IInvoker> invokers)
    //{
    //    if (invokers.Count == 0)
    //    {
    //        return;
    //    }
    //    //IInvoker invoker = invokers[0];
    //    //URL url = invoker.Url;
    //    //string providerApplication = url.GetRemoteApplication();

    //    //if (string.IsNullOrWhiteSpace(providerApplication))
    //    //{
    //    //    Logger().LogError("TagRouter must getConfig from or subscribe to a specific application, but the application " +
    //    //        "in this TagRouter is not specified.");
    //    //    return;
    //    //}

    //    //lock (this)
    //    //{
    //    //    if (!providerApplication.Equals(application))
    //    //    {
    //    //        if (!string.IsNullOrWhiteSpace(application))
    //    //        {
    //    //            this.GetRuleRepository().removeListener(application + RULE_SUFFIX, this);
    //    //        }
    //    //        String key = providerApplication + RULE_SUFFIX;
    //    //        this.GetRuleRepository().addListener(key, this);
    //    //        application = providerApplication;
    //    //        String rawRule = this.getRuleRepository().getRule(key, DynamicConfiguration.DEFAULT_GROUP);
    //    //        if (!string.IsNullOrWhiteSpace(rawRule))
    //    //        {
    //    //            this.process(new ConfigChangedEvent(key, DynamicConfiguration.DEFAULT_GROUP, rawRule));
    //    //        }
    //    //    }
    //    //}
    //}

    //public void Stop()
    //{
    //    if (!string.IsNullOrWhiteSpace(application))
    //    {
    //        //this.getRuleRepository().removeListener(application + RULE_SUFFIX, this);
    //    }
    //}
}
