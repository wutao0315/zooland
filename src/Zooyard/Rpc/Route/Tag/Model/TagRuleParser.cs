using System.Text.Json;

namespace Zooyard.Rpc.Route.Tag.Model;

public class TagRuleParser
{
    public static TagRouterRule Parse(string rawRule)
    {
        var map = JsonSerializer.Deserialize<Dictionary<string, object>>(rawRule)!;
        TagRouterRule rule = TagRouterRule.ParseFromMap(map);
        rule.RawRule= rawRule;
        if (rule.Tags.Count == 0)
        {
            rule.Valid = false;
        }

        rule.Init();
        return rule;
    }
}
