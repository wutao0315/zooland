using Microsoft.Extensions.Logging;
using Zooyard.Management;

namespace Zooyard.Rpc.Route.Condition.Config;

public class ServiceStateRouter(ILoggerFactory loggerFactory, IRpcStateLookup stateLookup, URL address)
    : ListenableStateRouter(loggerFactory, stateLookup, address, address.GetParameter("rule", "service"))
{
    public new const string NAME = "SERVICE_ROUTER";
}
