namespace Zooyard.Rpc.Route.Condition.Config;

public class ServiceStateRouter<T> : ListenableStateRouter<T>
{
    public const string NAME = "SERVICE_ROUTER";

    public ServiceStateRouter(URL url):base(url, "")
    {
        //super(url, DynamicConfiguration.getRuleKey(url));
    }
}
