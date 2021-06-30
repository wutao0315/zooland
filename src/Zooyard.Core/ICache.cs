namespace Zooyard.Core
{
    public interface ICache
    {
        T Get<T>(object key);
        void Put(object key,object value);
        void Clear();
    }
}
