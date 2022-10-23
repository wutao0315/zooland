namespace Zooyard.Rpc.Route.Condition.Config.Model;

public class ConditionRouterRule: AbstractRouterRule
{

    public static ConditionRouterRule parseFromMap(Dictionary<string, object> map)
    {
        var conditionRouterRule = new ConditionRouterRule();
        conditionRouterRule.parseFromMap0(map);

        if (map.TryGetValue(Constants.CONDITIONS_KEY, out object? conditions) &&  conditions is List<string> cds) 
        {
            conditionRouterRule.Conditions = cds;
        }

        //    Object conditions = map.get();
        //    if (conditions != null && List.class.isAssignableFrom(conditions.getClass())) {
        //    conditionRouterRule.setConditions(((List<Object>) conditions).stream()
        //            .map(String::valueOf).collect(Collectors.toList()));
        //}

        return conditionRouterRule;
    }



    public List<string> Conditions{get;set;}
}
