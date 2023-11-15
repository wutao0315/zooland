using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zooyard.Rpc.Route.Condition.Config;

public class AppStateRouter: ListenableStateRouter
{
    public new const string NAME = "APP_ROUTER";
    public AppStateRouter(ILoggerFactory loggerFactory, IOptionsMonitor<ZooyardOption> zooyard, URL address, string application) 
        : base(loggerFactory, zooyard, address, application) { }
}
