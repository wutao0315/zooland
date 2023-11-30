using Microsoft.Extensions.Options;
using Zooyard.Management;
using Zooyard.Rpc.Cache.Support;

namespace Zooyard.Rpc.Cache;

public class LruCache : ICache
{
    public const string NAME = "lru";
    public string Name => NAME;
    private readonly LruCacheData<object, object> _store;

    //public LruCache(IOptionsMonitor<ZooyardOption> zooyard)
    //{
    //    _store = new LruCacheData<object,object>(zooyard);
    //}
    public LruCache(IRpcStateLookup stateLookup)
    {
        _store = new LruCacheData<object, object>(stateLookup);
    }

    public T? Get<T>(object key)
    {
        var result = _store.GetObject(key);
        if (result is T val) 
        {
            return val;
        }
        return default;
    }

    public void Put(object key, object value)
    {
        _store.AddObject(key,value);
    }

    public void Clear()
    {
        _store.Clear();
    }
}
