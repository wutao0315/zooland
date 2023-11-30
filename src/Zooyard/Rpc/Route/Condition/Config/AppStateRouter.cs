using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooyard.Management;

namespace Zooyard.Rpc.Route.Condition.Config;

public class AppStateRouter: ListenableStateRouter
{
    public new const string NAME = "APP_ROUTER";
    public AppStateRouter(ILoggerFactory loggerFactory, IRpcStateLookup stateLookup, URL address, string application) 
        : base(loggerFactory, stateLookup, address, application) { }
}
