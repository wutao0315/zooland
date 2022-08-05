using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Condition
{
    internal class ConditionStateRouterFactory<T> : CacheableStateRouterFactory<T>
    {
        public const string NAME = "condition";

        protected override IStateRouter<T> createRouter(Type interfaceClass, URL url)
        {
            return new ConditionStateRouter<T>(url);
        }
    }
}
