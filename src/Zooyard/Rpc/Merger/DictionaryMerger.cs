namespace Zooyard.Rpc.Merger;

public class DictionaryMerger<K, V> : IMerger<IDictionary<K, V>>
    where K:notnull
{
    public string Name => "dictionary";
    public Type Type => typeof(IDictionary<,>);
    public IDictionary<K, V>? Merge(params IDictionary<K, V>[] items)
    {
        if (items.Length == 0)
        {
            return null;
        }
        var result = new Dictionary<K, V>();
        foreach (var item in items)
        {
            foreach (var data in item)
            {
                result[data.Key] = data.Value;
            }
        }
        return result;
    }
}
