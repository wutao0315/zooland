using Microsoft.Extensions.Options;
using System.Runtime.Caching;

namespace Zooyard.Rpc.Cache;

public class LocalCache : ICache
{
    private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    private readonly MemoryCache _store = MemoryCache.Default;

    public const string NAME = "local";
    public string Name => NAME;

    private int Timeout => _zooyard.CurrentValue.Meta.GetValue("cache.timeout", 60000);
    

    public LocalCache(IOptionsMonitor<ZooyardOption> zooyard)
    {
        _zooyard = zooyard;
    }

    public T Get<T>(object key)
    {
        var result = _store.Get(key.ToString());
        return (T)result;
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
