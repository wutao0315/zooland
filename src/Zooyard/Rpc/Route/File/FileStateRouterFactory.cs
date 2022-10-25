using Zooyard.Rpc.Route.Script;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route.File;

public class FileStateRouterFactory: IStateRouterFactory
{
    public const string NAME = "file";

    private IStateRouterFactory? routerFactory;

    public void ClearCache()
    {
        routerFactory?.ClearCache();
    }

    public string Name => NAME;

    public void SetRouterFactory(IStateRouterFactory routerFactory)
    {
        this.routerFactory = routerFactory;
    }

    public IStateRouter GetRouter(Type interfaceClass, URL address)
    {
        try
        {
            // Transform File URL into Script Route URL, and Load
            // file:///d:/path/to/route.js?router=script ==> script:///d:/path/to/route.js?type=js&rule=<file-content>
            string protocol = address.GetParameter(Constants.ROUTER_KEY, ScriptStateRouterFactory.NAME); // Replace original protocol (maybe 'file') with 'script'
            string? type = null; // Use file suffix to config script type, e.g., js, groovy ...
            string? path = address.Path;
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
            bool runtime = address.GetParameter(Constants.RUNTIME_KEY, false);
            URL script = URL.ValueOf(address.ToString())
                    .SetProtocol(protocol)
                    .AddParameter(Constants.TYPE_KEY, type)
                    .AddParameter(Constants.RUNTIME_KEY, runtime)
                    .AddParameterAndEncoded(Constants.RULE_KEY, rule);

            if (routerFactory == null) 
            {
                throw new Exception("please set route factory");
            }

            return routerFactory.GetRouter(interfaceClass, script);
        }
        catch (IOException e)
        {
            throw new Exception(e.Message, e);
        }
    }
}
