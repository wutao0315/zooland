namespace Zooyard.Rpc.Route.Condition.Config;

public class ServiceStateRouter : ListenableStateRouter
{
    public new const string NAME = "SERVICE_ROUTER";

    public ServiceStateRouter(URL address):base(address, address.GetParameter("rule","service"))
    {
    }
}
