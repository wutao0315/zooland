namespace Zooyard.Rpc;

public interface ICache
{
    string Name { get; }
    T? Get<T>(object key);
    void Put(object key, object value);
    void Clear();
}
