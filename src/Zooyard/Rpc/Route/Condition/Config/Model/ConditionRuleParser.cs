using System.Text.Json;

namespace Zooyard.Rpc.Route.Condition.Config.Model;

public class ConditionRuleParser
{
    /// <summary>
    /// 
    ///
    ///%YAML1.2
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
    public static ConditionRouterRule parse(string rawRule)
    {
        //Yaml yaml = new Yaml(new SafeConstructor());
        //Dictionary<String, Object> map = yaml.load(rawRule);
        var map = JsonSerializer.Deserialize<Dictionary<string, Object>>(rawRule);
        ConditionRouterRule rule = ConditionRouterRule.parseFromMap(map);
        rule.RawRule = rawRule;
        if (rule.Conditions.Count == 0)
        {
            rule.Valid = false;
        }

        return rule;
    }

}
