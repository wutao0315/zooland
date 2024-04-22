using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using Zooyard.Management;
using Zooyard.Rpc.Route.State;
using Zooyard.Rpc.Route.Tag.Model;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Tag;

public class TagStateRouter : AbstractStateRouter
{
    public const string NAME = "TAG_ROUTER";
    private const string TAG_KEY = "tag";
    private readonly ILogger _logger;
    //private static readonly string RULE_SUFFIX = ".tag-router";

    private TagRouterRule? tagRouterRule;
    private string? application;
    //private readonly IOptionsMonitor<ZooyardOption> _zooyard;

    //public TagStateRouter(ILoggerFactory loggerFactory, IOptionsMonitor<ZooyardOption> zooyard, URL address) : base(address)
    //{
    //    _logger = loggerFactory.CreateLogger<TagStateRouter>();
    //    _zooyard = zooyard;
    //    _zooyard.OnChange(OnChanged);
    //}

    private readonly IRpcStateLookup _stateLookup;

    public TagStateRouter(ILoggerFactory loggerFactory, IRpcStateLookup stateLookup, URL address) : base(address)
    {
        _logger = loggerFactory.CreateLogger<TagStateRouter>();
        _stateLookup = stateLookup;
        _stateLookup.OnChange(OnChanged);
    }

    // 监听配置或者服务注册变化，清空缓存
    void OnChanged(IRpcStateLookup value)
    {
        if (string.IsNullOrWhiteSpace(application)) 
        {
            return;
        }

        if (!_stateLookup.TryGetService(application, out var serviceOption))
        {
            return;
        }

        serviceOption.Model.Config.Metadata.TryGetValue("route.rule", out var ruleContent);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"Notification of tag rule, change type is: {value}, raw rule is:\n {ruleContent}");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(ruleContent))
            {
                this.tagRouterRule = null;
            }
            else 
            {
                this.tagRouterRule = TagRuleParser.Parse(ruleContent);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to parse the raw tag router rule and it will not take effect, please check if the rule matches with the template, the raw rule is:\n ");
        }
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

    protected override IList<URL> DoRoute(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder, Holder<String> messageHolder)
    {
        if (invokers.Count == 0)
        {
            if (needToPrintMessage)
            {
                messageHolder.Value = "Directly Return. Reason: Invokers from previous router is empty.";
            }
            return invokers;
        }

        // since the rule can be changed by config center, we should copy one to use.
        TagRouterRule? tagRouterRuleCopy = tagRouterRule;
        if (tagRouterRuleCopy == null || !tagRouterRuleCopy.Valid || !tagRouterRuleCopy.Enabled)
        {
            if (needToPrintMessage)
            {
               messageHolder.Value = "Disable Tag Router. Reason: tagRouterRule is invalid or disabled";
            }
            return FilterUsingStaticTag(invokers, address, invocation);
        }

        IList<URL> result = invokers;
        string? tag = string.IsNullOrWhiteSpace(invocation.GetAttachment(TAG_KEY)) ? address.GetParameter(TAG_KEY)! : invocation.GetAttachment(TAG_KEY);

        // if we are requesting for a Provider with a specific tag
        if (!string.IsNullOrWhiteSpace(tag))
        {
            tagRouterRuleCopy.TagnameToAddresses.TryGetValue(tag, out List<string>? addresses);
            // filter by dynamic tag group first
            if (addresses!= null && addresses.Count > 0)
            {
                result = FilterInvoker(invokers, invoker=>AddressMatches(invoker, addresses));
                // if result is not null OR it's null but force=true, return result directly
                if (result.Count > 0 || tagRouterRuleCopy.Force)
                {
                    if (needToPrintMessage)
                    {
                        messageHolder.Value = $"Use tag {tag} to route. Reason: result is not null OR it's null but force=true";
                    }
                    return result;
                }
            }
            else
            {
                // dynamic tag group doesn't have any item about the requested app OR it's null after filtered by
                // dynamic tag group but force=false. check static tag
                result = FilterInvoker(invokers, invoker=>tag.Equals(invoker.GetParameter(TAG_KEY)));
            }
            // If there's no tagged providers that can match the current tagged request. force.tag is set by default
            // to false, which means it will invoke any providers without a tag unless it's explicitly disallowed.
            if (result.Count > 0 || IsForceUseTag(invocation))
            {
                if (needToPrintMessage)
                {
                    messageHolder.Value = $"Use tag {tag} to route. Reason: result is not empty or ForceUseTag key is true in invocation";
                }
                return result;
            }
            // FAILOVER: return all Providers without any tags.
            else
            {
                var tmp = FilterInvoker(invokers, invoker=>AddressNotMatches(invoker, tagRouterRuleCopy.Addresses));
                if (needToPrintMessage)
                {
                    messageHolder.Value = "FAILOVER: return all Providers without any tags";
                }
                return FilterInvoker(tmp, invoker=>string.IsNullOrWhiteSpace(invoker.GetParameter(TAG_KEY)));
            }
        }
        else
        {
            // List<String> addresses = tagRouterRule.filter(providerApp);
            // return all addresses in dynamic tag group.
            List<string> addresses = tagRouterRuleCopy.Addresses;
            if (addresses?.Count > 0)
            {
                result = FilterInvoker(invokers, invoker=>AddressNotMatches(invoker, addresses));
                // 1. all addresses are in dynamic tag group, return empty list.
                if (result.Count == 0)
                {
                    if (needToPrintMessage)
                    {
                        messageHolder.Value = "all addresses are in dynamic tag group, return empty list";
                    }
                    return result;
                }
                // 2. if there are some addresses that are not in any dynamic tag group, continue to filter using the
                // static tag group.
            }
            if (needToPrintMessage)
            {
                messageHolder.Value = "filter using the static tag group";
            }
            return FilterInvoker(result, invoker=> 
            {
                string? localTag = invoker.GetParameter(TAG_KEY);
                return string.IsNullOrWhiteSpace(localTag) || !tagRouterRuleCopy.GetTagNames().Contains(localTag);
            });
        }
    }


    /// <summary>
    /// 
    /// If there's no dynamic tag rule being set, use static tag in URL.
    /// 
    /// A typical scenario is a Consumer using version 2.7.x calls Providers using version 2.6.x or lower,
    /// the Consumer should always respect the tag in provider URL regardless of whether a dynamic tag rule has been set to it or not.
    /// 
    /// TODO, to guarantee consistent behavior of interoperability between 2.6- and 2.7+, this method should has the same logic with the TagRouter in 2.6.x.
    /// 
    /// </summary>
    /// <param name="invokers"></param>
    /// <param name="url"></param>
    /// <param name="invocation"></param>
    /// <returns></returns>
    private IList<URL> FilterUsingStaticTag(IList<URL> invokers, URL url, IInvocation invocation)
    {
        IList<URL>? result = null;
        // Dynamic param
        string? tag = string.IsNullOrWhiteSpace(invocation.GetAttachment(TAG_KEY)) ? url.GetParameter(TAG_KEY)! :invocation.GetAttachment(TAG_KEY);
        // Tag request
        if (!string.IsNullOrWhiteSpace(tag))
        {
            result = FilterInvoker(invokers, invoker => tag.Equals(invoker.GetParameter(TAG_KEY)));
            if (result.Count == 0 && !IsForceUseTag(invocation))
            {
                result = FilterInvoker(invokers, invoker=>string.IsNullOrWhiteSpace(invoker.GetParameter(TAG_KEY)));
            }
        }
        else
        {
            result = FilterInvoker(invokers, invoker => string.IsNullOrWhiteSpace(invoker.GetParameter(TAG_KEY)));
        }
        return result;
    }

    public override bool Runtime => tagRouterRule != null && tagRouterRule.Runtime;
    public override bool Force => tagRouterRule != null && tagRouterRule.Force;

    private bool IsForceUseTag(IInvocation invocation)
    {
        return bool.Parse(invocation.GetAttachment(Constants.FORCE_USE_TAG, this.Address.GetParameter(Constants.FORCE_USE_TAG, "false"))!);
    }

    private IList<URL> FilterInvoker(IList<URL> invokers, Func<URL, bool> predicate)
    {
        if (invokers.Count(predicate) == invokers.Count())
        {
            return invokers;
        }

        var newInvokers = invokers.Where(predicate).ToList();
        return newInvokers;
    }

    private bool AddressMatches(URL url, List<String> addresses)
    {
        return addresses != null && CheckAddressMatch(addresses, url.Host, url.Port);
    }

    private bool AddressNotMatches(URL url, List<String> addresses)
    {
        return addresses == null || !CheckAddressMatch(addresses, url.Host, url.Port);
    }

    private bool CheckAddressMatch(List<String> addresses, String host, int port)
    {
        foreach (var address in addresses)
        {
            try
            {
                if (NetUtil.MatchIpExpression(address, host, port))
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
                _logger.LogError(e, "The format of ip address is invalid in tag route. Address :" + address);
            }
        }
        return false;
    }

    public void SetApplication(string app)
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
