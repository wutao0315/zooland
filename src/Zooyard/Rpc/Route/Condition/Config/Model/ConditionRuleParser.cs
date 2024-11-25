using System.Text.Json;

namespace Zooyard.Rpc.Route.Condition.Config.Model;

public class ConditionRuleParser
{
    /// <summary>
    ///
    ///Json
    ///
    ///scope: application
    ///runtime: true
    ///force: false
    ///conditions:
    ///  - >
    ///    method!=sayHello =>
    ///  - >
    ///    ip=127.0.0.1
    ///    =>
    ///    1.1.1.1
    /// </summary>
    /// <param name="rawRule"></param>
    /// <returns></returns>
    public static ConditionRouterRule Parse(string rawRule)
    {
        var map = JsonSerializer.Deserialize<Dictionary<string, object>>(rawRule)!;
        ConditionRouterRule rule = ConditionRouterRule.ParseFromMap(map);
        rule.RawRule = rawRule;
        if (rule.Conditions.Count == 0)
        {
            rule.Valid = false;
        }

        return rule;
    }

}
