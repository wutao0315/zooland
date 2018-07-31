using System.Collections.Generic;
using System.Threading;
using Zooyard.Core;

namespace Zooyard.Rpc.Cache
{
    public class ThreadLocalCache : ICache
    {
        public const string NAME = "threadlocal";
        private ThreadLocal<IDictionary<object, object>> store;
        public ThreadLocalCache(URL url)
        {
            this.store = new ThreadLocal<IDictionary<object, object>>(() => new Dictionary<object, object>());
        }

        public void Put(object key, object value)
        {
            store.Value.Add(key, value);
        }

        public object Get(object key)
        {
            return store.Value[key];
        }

        public void Clear()
        {
            store.Value.Clear();
        }
    }
}
