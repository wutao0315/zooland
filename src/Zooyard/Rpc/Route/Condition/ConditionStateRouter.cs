using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
//using Zooyard.Logging;
using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Condition;

public class ConditionStateRouter : AbstractStateRouter
{
    public static readonly string NAME = "condition";

    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ConditionStateRouter));
    private readonly ILogger _logger;
    protected static Regex ROUTE_PATTERN = new("([&!=,]*)\\s*([^&!=,\\s]+)", RegexOptions.Compiled);
    protected static Regex ARGUMENTS_PATTERN = new("arguments\\[([0-9]+)\\]", RegexOptions.Compiled);
    protected Dictionary<String, MatchPair>? whenCondition;
    protected Dictionary<String, MatchPair>? thenCondition;

    private readonly bool _enabled;
    public ConditionStateRouter(ILoggerFactory loggerfactory, URL address)
        :base(address)
    {
        _logger = loggerfactory.CreateLogger<ConditionStateRouter>();
        this.Force = address.GetParameter(Constants.FORCE_KEY, false);
        _enabled = address.GetParameter(Constants.ENABLED_KEY, true);
        if (_enabled)
        {
            Init(address.GetParameterAndDecoded(Constants.RULE_KEY));
        }
    }
    public ConditionStateRouter(ILoggerFactory loggerfactory, URL address, string rule, bool force, bool enabled):this(loggerfactory, address)
    {
        this.Force = force;
        _enabled = enabled;
        if (enabled)
        {
            this.Init(rule);
        }
    }

   

    public void Init(string rule)
    {
        try
        {
            if (rule == null || rule.Trim().Length == 0)
            {
                throw new ArgumentException("Illegal route rule!");
            }
            rule = rule.Replace("consumer.", "").Replace("provider.", "");
            int i = rule.IndexOf("=>");
            string? whenRule = i < 0 ? null : rule.Substring(0, i).Trim();
            string thenRule = i < 0 ? rule.Trim() : rule.Substring(i + 2).Trim();
            Dictionary<string, MatchPair> when = string.IsNullOrWhiteSpace(whenRule) || "true".Equals(whenRule) ? new Dictionary<string, MatchPair>() : ParseRule(whenRule);
            Dictionary<string, MatchPair>? then = string.IsNullOrWhiteSpace(thenRule) || "false".Equals(thenRule) ? null : ParseRule(thenRule);
            // NOTE: It should be determined on the business level whether the `When condition` can be empty or not.
            this.whenCondition = when;
            this.thenCondition = then;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message, e);
        }
    }

    private static Dictionary<string, MatchPair> ParseRule(string rule)
    {
        var condition = new Dictionary<string, MatchPair>();
        if (string.IsNullOrWhiteSpace(rule))
        {
            return condition;
        }
        // Key-Value pair, stores both match and mismatch conditions
        MatchPair? pair = null;
        // Multiple values
        HashSet<string>? values = null;

        var matcher = ROUTE_PATTERN.Match(rule);

        while (matcher != null)
        { 
            // Try to match one by one
            string separator = matcher.Groups[1].Value;
            string content = matcher.Groups[2].Value;
            // Start part of the condition expression.
            if (string.IsNullOrWhiteSpace(separator))
            {
                pair = new MatchPair();
                condition.Add(content, pair);
            }
            // The KV part of the condition expression
            else if ("&".Equals(separator))
            {
                if (!condition.TryGetValue(content, out MatchPair? val) || val == null)
                {
                    pair = new MatchPair();
                    condition.Add(content, pair);
                }
                else
                {
                    pair = val;
                }
            }
            // The Value in the KV part.
            else if ("=".Equals(separator))
            {
                if (pair == null)
                {
                    throw new Exception($"Illegal route rule \"{rule}\", The error char '{separator}' at index {matcher.Index} before \""
                            + content + "\".");
                }

                values = pair.matches;
                values.Add(content);
            }
            // The Value in the KV part.
            else if ("!=".Equals(separator))
            {
                if (pair == null)
                {
                    throw new Exception($"Illegal route rule \"{rule}\", The error char '{separator}' at index {matcher.Index} before \"{content}\".");
                }

                values = pair.mismatches;
                values.Add(content);
            }
            // The Value in the KV part, if Value have more than one items.
            else if (",".Equals(separator))
            { // Should be separated by ','
                if (values == null || values.Count == 0)
                {
                    throw new Exception($"Illegal route rule \"{rule}\", The error char '{separator}' at index {matcher.Index} before \"{content}\".");
                }
                values.Add(content);
            }
            else
            {
                throw new Exception($"Illegal route rule \"{rule}\", The error char '{separator}' at index {matcher.Index} before \"{content}\".");
            }

            matcher = matcher.NextMatch();
        }
        return condition;
    }

    protected override IList<URL> DoRoute(IList<URL> invokers, URL url, IInvocation invocation,
                                              bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder, Holder<String> messageHolder)
    {
        if (!_enabled)
        {
            if (needToPrintMessage)
            {
                messageHolder.Value = "Directly return. Reason: ConditionRouter disabled.";
            }
            return invokers;
        }

        if (invokers.Count == 0)
        {
            if (needToPrintMessage)
            {
                messageHolder.Value = "Directly return. Reason: Invokers from previous router is empty.";
            }
            return invokers;
        }
        try
        {
            if (!MatchWhen(url, invocation))
            {
                if (needToPrintMessage)
                {
                    messageHolder.Value = "Directly return. Reason: WhenCondition not match.";
                }
                return invokers;
            }
            if (thenCondition == null)
            {
                _logger.LogWarning($"The current consumer in the service blacklist. consumer: {NetUtil.LocalHost}, service: {url.ServiceKey}");
                if (needToPrintMessage)
                {
                    messageHolder.Value = "Empty return. Reason: ThenCondition is empty.";
                }
                return new List<URL>();
            }

            var result = invokers.Where(w=> !MatchThen(w, url)).ToList();

            if (result.Count>0)
            {
                if (needToPrintMessage)
                {
                    messageHolder.Value = "Match return.";
                }
                return result;
            }
            else if (this.Force)
            {
                _logger.LogWarning($"The route result is empty and force execute. consumer: {NetUtil.LocalHost}, service: {url.ServiceKey}, router: {url.GetParameterAndDecoded(Constants.RULE_KEY)}");

                if (needToPrintMessage)
                {
                    messageHolder.Value = "Empty return. Reason: Empty result from condition and condition is force.";
                }
                return result;
            }
        }
        catch (Exception t)
        {
            _logger.LogError(t, $"Failed to execute condition router rule: {Address}, invokers: {invokers}, cause: {t.Message}");
        }
        if (needToPrintMessage)
        {
            messageHolder.Value = "Directly return. Reason: Error occurred ( or result is empty ).";
        }
        return invokers;
    }

    // We always return true for previously defined Router, that is, old Router doesn't support cache anymore.
    //        return true;
    public override bool Runtime => this.Address.GetParameter(Constants.RUNTIME_KEY, false);

    bool MatchWhen(URL url, IInvocation invocation)
    {
        return whenCondition!.Count==0 || MatchCondition(whenCondition, url, null, invocation);
    }

    private bool MatchThen(URL url, URL param)
    {
        return thenCondition!.Count>0 && MatchCondition(thenCondition, url, param, null);
    }

    private bool MatchCondition(Dictionary<string, MatchPair> condition, URL url, URL? param, IInvocation? invocation)
    {
        IDictionary<String, String> sample = url.ToMap();
        bool result = false;
        foreach (var matchPair in condition)
        {
            string key = matchPair.Key;

            if (key.StartsWith(Constants.ARGUMENTS))
            {
                if (!MatchArguments(matchPair, invocation!))
                {
                    return false;
                }
                else
                {
                    result = true;
                    continue;
                }
            }

            string? sampleValue;
            //get real invoked method name from invocation
            if (invocation != null && (CommonConstants.METHOD_KEY.Equals(key) || CommonConstants.METHODS_KEY.Equals(key)))
            {
                sampleValue = invocation.MethodInfo.Name; 
            }
            else if (Constants.ADDRESS_KEY.Equals(key))
            {
                sampleValue = url.Address;
            }
            else if (CommonConstants.HOST_KEY.Equals(key))
            {
                sampleValue = url.Host;
            }
            else
            {
                sampleValue = sample[key];
            }
            if (sampleValue != null)
            {
                if (!matchPair.Value.IsMatch(sampleValue, param))
                {
                    return false;
                }
                else
                {
                    result = true;
                }
            }
            else
            {
                //not pass the condition
                if (!(matchPair.Value.matches.Count==0))
                {
                    return false;
                }
                else
                {
                    result = true;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// analysis the arguments in the rule.
    /// Examples would be like this:
    /// "arguments[0]=1", whenCondition is that the first argument is equal to '1'.
    /// "arguments[1]=a", whenCondition is that the second argument is equal to 'a'.
    /// 
    /// </summary>
    /// <param name="matchPair"></param>
    /// <param name="invocation"></param>
    /// <returns></returns>
    private bool MatchArguments(KeyValuePair<string, MatchPair> matchPair, IInvocation invocation)
    {
        try
        {
            // split the rule
            string key = matchPair.Key;
            string[] expressArray = key.Split("\\.");
            string argumentExpress = expressArray[0];
            Match matcher = ARGUMENTS_PATTERN.Match(argumentExpress);
            if (matcher == null)
            {
                return false;
            }

            //extract the argument index
            int index = int.Parse(matcher.Groups[1].Value);
            if (index < 0 || index > invocation.Arguments.Length)
            {
                return false;
            }

            //extract the argument value
            object obj = invocation.Arguments[index];

            if (matchPair.Value.IsMatch(obj.ToString(), null))
            {
                return true;
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, $"Arguments match failed, matchPair[]{matchPair}] invocation[{invocation}]");
        }

        return false;
    }

    protected sealed class MatchPair
    {
        internal readonly HashSet<string> matches = new();
        internal readonly HashSet<string> mismatches = new();

        internal bool IsMatch(string? value, URL? param)
        {
            if (matches.Count > 0 && mismatches.Count > 0)
            {
                foreach (string match in matches)
                {
                    if (UrlUtils.IsMatchGlobPattern(match, value, param))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mismatches.Count > 0 && matches.Count == 0)
            {
                foreach (string mismatch in mismatches)
                {
                    if (UrlUtils.IsMatchGlobPattern(mismatch, value, param))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (matches.Count > 0 && mismatches.Count > 0)
            {
                //when both mismatches and matches contain the same value, then using mismatches first
                foreach (string mismatch in mismatches)
                {
                    if (UrlUtils.IsMatchGlobPattern(mismatch, value, param))
                    {
                        return false;
                    }
                }
                foreach (string match in matches)
                {
                    if (UrlUtils.IsMatchGlobPattern(match, value, param))
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }
    }
}
