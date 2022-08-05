using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.Route.Condition.Config.Model
{
    public class ConditionRouterRule: AbstractRouterRule
    {
        public ConditionRouterRule()
        {
        }

        public static ConditionRouterRule parseFromMap(Dictionary<string, object> map)
        {
            ConditionRouterRule conditionRouterRule = new ConditionRouterRule();
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

  

        public List<String> Conditions{get;set;}
    }
}
