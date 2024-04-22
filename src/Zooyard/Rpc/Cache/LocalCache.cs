using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Zooyard.Management;

namespace Zooyard.Rpc.Cache;

public class LocalCache(IMemoryCache _memoryCache, IRpcStateLookup _proxyState) : ICache
{
    //private readonly IOptionsMonitor<ZooyardServiceOption> _zooyard;
    //private readonly MemoryCache _store = MemoryCache.Default;
    private CancellationTokenSource _resetCacheToken = new();

    public const string NAME = "local";
    public string Name => NAME;

    //private int Timeout => _zooyard.CurrentValue.Meta.GetValue("cache.timeout", 60000);

    private int Timeout => _proxyState.GetGlobalMataValue("cache.timeout", 60000);


    //public LocalCache(IMemoryCache memoryCache, IOptionsMonitor<ZooyardOption> zooyard)
    //{
    //    _memoryCache = memoryCache;
    //    _zooyard = zooyard;
    //}

    public T? Get<T>(object key)
    {
        var result = _memoryCache.Get(key.ToString()??"");
        if (result == null) 
        {
            return default;
        }
        return (T)result;
    }

    public void Put(object key, object value)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(Timeout)
        };

        using var entry = _memoryCache.CreateEntry(key);
        entry.SetOptions(options);
        entry.Value = value;

        // add an expiration token that allows us to clear the entire cache with a single method call
        entry.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
    }

    public void Clear()
    {
        _resetCacheToken.Cancel(); // this triggers the CancellationChangeToken to expire every item from cache
        _resetCacheToken.Dispose(); // dispose the current cancellation token source and create a new one
        _resetCacheToken = new CancellationTokenSource();

    }
}
