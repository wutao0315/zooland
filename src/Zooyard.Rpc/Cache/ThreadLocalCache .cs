using System.Collections.Generic;
using System.Threading;
using Zooyard.Core;

namespace Zooyard.Rpc.Cache
{
    public class ThreadLocalCache : ICache
    {
        public const string NAME = "threadlocal";
        private readonly ThreadLocal<IDictionary<object, object>> _store = new ThreadLocal<IDictionary<object, object>>(() => new Dictionary<object, object>());
        public void Put(object key, object value)
        {
            _store.Value.Add(key, value);
        }

        public object Get(object key)
        {
            return _store.Value[key];
        }

        public void Clear()
        {
            _store.Value.Clear();
        }
    }
}
