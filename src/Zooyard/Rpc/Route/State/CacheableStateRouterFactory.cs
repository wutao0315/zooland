using System.Collections.Concurrent;

namespace Zooyard.Rpc.Route.State;

public abstract class CacheableStateRouterFactory: IStateRouterFactory
{
  
    private readonly ConcurrentDictionary<string, IStateRouter> routerMap = new ();

    public IStateRouter GetRouter(Type interfaceClass, URL address)
    {
        return routerMap.GetOrAdd(address.ServiceKey!, CreateRouter(interfaceClass, address));
    }
    public void ClearCache() 
    {
        routerMap.Clear();
    }
    public abstract string Name { get;}
    protected abstract IStateRouter CreateRouter(Type interfaceClass, URL address);
}
