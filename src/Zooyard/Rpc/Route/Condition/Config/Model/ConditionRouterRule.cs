using System.Collections;

namespace Zooyard.Rpc.Route.Condition.Config.Model;

public record ConditionRouterRule : AbstractRouterRule
{

    public static ConditionRouterRule ParseFromMap(Dictionary<string, object> map)
    {
        var conditionRouterRule = new ConditionRouterRule();
        conditionRouterRule.ParseFromMapInner(map);

        if (map.TryGetValue(Constants.CONDITIONS_KEY, out object? conditions) 
            && typeof(IEnumerable).IsAssignableFrom(conditions.GetType())) 
        {
            var cds = new List<string>();
            foreach (var item in (IEnumerable)conditions)
            {
                cds.Add(item.ToString()!);
            }
            conditionRouterRule.Conditions = cds;
        }

        return conditionRouterRule;
    }



    public List<string> Conditions { get; set; } = new();
}
