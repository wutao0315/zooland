using System.Collections.Generic;
using System.Threading;
using Zooyard;

namespace Zooyard.Rpc.Cache
{
    public class ThreadLocalCache : ICache
    {
        public const string NAME = "threadlocal";
        private readonly ThreadLocal<IDictionary<object, object>> _store = new ThreadLocal<IDictionary<object, object>>(() => new Dictionary<object, object>());
        public ThreadLocalCache(URL url)
        {
        }
        public void Put(object key, object value)
        {
            _store.Value.Add(key, value);
        }

        public T Get<T>(object key)
        {
            var result = _store.Value[key];
            return (T)result;
        }

        public void Clear()
        {
            _store.Value.Clear();
        }
    }
}
