namespace Zooyard.Rpc.Route.Condition.Config;

public class AppStateRouter: ListenableStateRouter
{
    public new const string NAME = "APP_ROUTER";

    public AppStateRouter(URL address,string application):base(address, application)
    {
    }
}
