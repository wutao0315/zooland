using Zooyard.Rpc.Cache.Support;

namespace Zooyard.Rpc.Cache;

public class LruCache : ICache
{
    public const string NAME = "lru";
    private readonly LruCacheData<object, object> _store;

    public LruCache(URL url)
    {
        var max = url.GetParameter("cache.size", 1000);
        var memoryRefreshInterval = url.GetParameter("cache.interval", 1000);
        var itemExpiryTimeout = url.GetParameter("cache.timeout", 60000);
        _store = new LruCacheData<object,object>(itemExpiryTimeout, max, memoryRefreshInterval);
    }

    public T Get<T>(object key)
    {
        var result = _store.GetObject(key);
        return (T)result;
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
