using Zooyard.Core;
using Zooyard.Rpc.Cache.Support;

namespace Zooyard.Rpc.Cache
{
    public class LruCache : ICache
    {
        public const string NAME = "lru";
        private LruCacheData<object, object> store;

        public LruCache(URL url)
        {
            var max = url.GetParameter("cache.size", 1000);
            var memoryRefreshInterval = url.GetParameter("cache.interval", 1000);
            var itemExpiryTimeout = url.GetParameter("cache.timeout", 60000);
            this.store = new LruCacheData<object,object>(itemExpiryTimeout, max, memoryRefreshInterval);
        }

        public object Get(object key)
        {
            return store.GetObject(key);
        }

        public void Put(object key, object value)
        {
            store.AddObject(key,value);
        }

        public void Clear()
        {
            store.Clear();
        }
    }
}
