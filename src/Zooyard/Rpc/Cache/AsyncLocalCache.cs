namespace Zooyard.Rpc.Cache;

public class AsyncLocalCache : ICache
{
    public const string NAME = "Asynclocal";
    private readonly AsyncLocal<IDictionary<object, object>> _store = new ();
    public AsyncLocalCache(URL url)
    {
    }
    public void Put(object key, object value)
    {
        if (_store.Value == null) 
        {
            _store.Value = new  Dictionary<object, object> ();
        }
        _store.Value.Add(key, value);
    }

    public T Get<T>(object key)
    {
        if (_store.Value == null)
        {
            _store.Value = new Dictionary<object, object>();
        }

        if (_store.Value.TryGetValue(key, out object? value) && value != null)
        {
            return (T)value;
        }
        return default!;
    }

    public void Clear()
    {
        _store.Value?.Clear();
    }
}
