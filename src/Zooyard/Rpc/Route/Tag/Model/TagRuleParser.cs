using System.Text.Json;

namespace Zooyard.Rpc.Route.Tag.Model;

public class TagRuleParser
{
    public static TagRouterRule parse(string rawRule)
    {
        //Yaml yaml = new Yaml(new SafeConstructor());
        //Dictionary<String, Object> map = yaml.load(rawRule);
        var map = JsonSerializer.Deserialize<Dictionary<string, object>>(rawRule)!;
        //Dictionary<String, Object> map = new();
        TagRouterRule rule = TagRouterRule.parseFromMap(map);
        rule.RawRule= rawRule;
        if (rule.Tags.Count == 0)
        {
            rule.Valid = false;
        }

        rule.init();
        return rule;
    }
}
