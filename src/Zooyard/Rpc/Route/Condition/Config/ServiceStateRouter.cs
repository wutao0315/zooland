using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zooyard.Rpc.Route.Condition.Config;

public class ServiceStateRouter : ListenableStateRouter
{
    public new const string NAME = "SERVICE_ROUTER";
    public ServiceStateRouter(ILoggerFactory loggerFactory, IOptionsMonitor<ZooyardOption> zooyard, URL address)
        : base(loggerFactory, zooyard, address, address.GetParameter("rule", "service")) { }
}
