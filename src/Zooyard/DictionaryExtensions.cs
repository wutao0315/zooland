namespace Zooyard;

public static class DictionaryExtensions
{
    public static void PutAll<T, V>(this IDictionary<T, V> value, IDictionary<T, V> other) 
    {
        foreach (var item in other)
        {
            if (value.ContainsKey(item.Key))
            {
                value.Remove(item.Key);
            }
            value.Add(item.Key, item.Value);
        }
    }
}
