namespace Zooyard.Rpc.Route.Condition.Config;

public class AppStateRouter<T>: ListenableStateRouter<T>
{
    public const string NAME = "APP_ROUTER";

    public AppStateRouter(URL url):base(url, url.Application)
    {
    }
}
