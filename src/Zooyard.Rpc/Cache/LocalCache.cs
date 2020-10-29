using System;
using System.Runtime.Caching;
using Zooyard.Core;

namespace Zooyard.Rpc.Cache
{
    public class LocalCache : ICache
    {
        public const string NAME = "local";
        private readonly MemoryCache _store = MemoryCache.Default;
        private int Timeout { get; set; }

        public LocalCache(URL url)
        {
            this.Timeout = url.GetParameter("cache.timeout", 60000);
        }

        public object Get(object key)
        {
            return _store.Get(key.ToString());
        }

        public void Put(object key, object value)
        {
            _store.Add(key.ToString(), value, DateTimeOffset.Now.AddMilliseconds(Timeout));
        }

        public void Clear()
        {
            foreach (var item in _store.GetValues(null))
            {
                _store.Remove(item.Key);
            }
        }
    }
}
