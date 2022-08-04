namespace Zooyard.Rpc.Cache;

public class ThreadLocalCache : ICache
{
    public const string NAME = "threadlocal";
    private readonly ThreadLocal<IDictionary<object, object>> _store = new (() => new Dictionary<object, object>());
    public ThreadLocalCache(URL url)
    {
    }
    public void Put(object key, object value)
    {
        _store.Value!.Add(key, value);
    }

    public T Get<T>(object key)
    {
        if (_store.Value!.TryGetValue(key, out object? value) && value != null) 
        {
            return (T)value;
        }
        return default!;
    }

    public void Clear()
    {
        _store.Value!.Clear();
    }
}
