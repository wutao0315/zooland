using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Rpc.Route.Script;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.File
{
    public class FileStateRouterFactory<T>: IStateRouterFactory<T>
    {
        public const string NAME = "file";

        private IStateRouterFactory<T> routerFactory;

        public void SetRouterFactory(IStateRouterFactory<T> routerFactory)
        {
            this.routerFactory = routerFactory;
        }

        public IStateRouter<T> GetRouter(Type interfaceClass, URL url)
        {
            try
            {
                // Transform File URL into Script Route URL, and Load
                // file:///d:/path/to/route.js?router=script ==> script:///d:/path/to/route.js?type=js&rule=<file-content>
                string protocol = url.GetParameter(Constants.ROUTER_KEY, ScriptStateRouterFactory<T>.NAME); // Replace original protocol (maybe 'file') with 'script'
                string? type = null; // Use file suffix to config script type, e.g., js, groovy ...
                string? path = url.Path;
                if (path != null)
                {
                    int i = path.LastIndexOf('.');
                    if (i > 0)
                    {
                        type = path.Substring(i + 1);
                    }
                }

                string rule = "";// IOUtils.read(new FileReader(url.getAbsolutePath()));

                // FIXME: this code looks useless
                bool runtime = url.GetParameter(Constants.RUNTIME_KEY, false);
                URL script = URL.ValueOf(url.ToString())
                        .SetProtocol(protocol)
                        .AddParameter(Constants.TYPE_KEY, type)
                        .AddParameter(Constants.RUNTIME_KEY, runtime)
                        .AddParameterAndEncoded(Constants.RULE_KEY, rule);

                return routerFactory.GetRouter(interfaceClass, script);
            }
            catch (IOException e)
            {
                throw new Exception(e.Message, e);
            }
        }
    }
}
