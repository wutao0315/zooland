namespace Zooyard.Core
{
    public interface ICache
    {
        object Get(object key);
        void Put(object key,object value);
        void Clear();
    }
}
