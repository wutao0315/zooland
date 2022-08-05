using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.Script;

public class ScriptStateRouterFactory<T>: IStateRouterFactory<T>
{
    public const string NAME = "script";

    public IStateRouter<T> getRouter(Type interfaceClass, URL url)
    {
        return new ScriptStateRouter<T>(url);
    }
}
