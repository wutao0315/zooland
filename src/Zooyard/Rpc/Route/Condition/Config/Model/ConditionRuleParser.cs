namespace Zooyard.Rpc.Route.Condition.Config.Model;

public class ConditionRuleParser
{
    public static ConditionRouterRule parse(string rawRule)
    {
        //Yaml yaml = new Yaml(new SafeConstructor());
        //Dictionary<String, Object> map = yaml.load(rawRule);
        Dictionary<String, Object> map = new();
        ConditionRouterRule rule = ConditionRouterRule.parseFromMap(map);
        rule.RawRule = rawRule;
        if (rule.Conditions.Count == 0)
        {
            rule.Valid = false;
        }

        return rule;
    }

}
