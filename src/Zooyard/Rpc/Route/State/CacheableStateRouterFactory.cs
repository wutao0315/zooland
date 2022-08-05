using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.Route.State
{
    public abstract class CacheableStateRouterFactory<T>: IStateRouterFactory<T>
    {
        private readonly ConcurrentDictionary<string, IStateRouter<T>> routerMap = new ();

        public IStateRouter<T> getRouter(Type interfaceClass, URL url)
        {
            return routerMap.GetOrAdd(url.ServiceKey, createRouter(interfaceClass, url));
            //return routerMap.computeIfAbsent(url.getServiceKey(), k->createRouter(interfaceClass, url));
        }

        protected abstract IStateRouter<T> createRouter(Type interfaceClass, URL url);
    }
}
