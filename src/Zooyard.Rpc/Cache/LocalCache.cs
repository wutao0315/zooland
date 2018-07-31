using System;
using System.Runtime.Caching;
using Zooyard.Core;

namespace Zooyard.Rpc.Cache
{
    public class LocalCache:ICache
    {
        public const string NAME = "local";
        private MemoryCache store;
        private int Timeout { get; set; }

        public LocalCache(URL url)
        {
            this.Timeout=url.GetParameter("cache.timeout", 60000);
            this.store = MemoryCache.Default;
        }

        public object Get(object key)
        {
            return store.Get(key.ToString());
        }

        public void Put(object key, object value)
        {
            store.Add(key.ToString(), value, DateTimeOffset.Now.AddMilliseconds(Timeout));
        }

        public void Clear()
        {
            foreach (var item in store.GetValues(null))
            {
                store.Remove(item.Key);
            }
        }
    }
}
