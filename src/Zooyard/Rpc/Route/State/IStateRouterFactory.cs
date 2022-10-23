namespace Zooyard.Rpc.Route.State;

public interface IStateRouterFactory
{
    string Name { get; }
    //Adaptive("protocol")
    /// <summary>
    /// Create state router.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="interfaceClass"></param>
    /// <param name="address"></param>
    /// <returns></returns>
    IStateRouter GetRouter(Type interfaceClass, URL address);
}
