namespace Zooyard.Rpc.Route.State;

public interface IStateRouterFactory<T>
{

    //Adaptive("protocol")
    /// <summary>
    /// Create state router.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="interfaceClass"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    IStateRouter<T> GetRouter(Type interfaceClass, URL url);
}
