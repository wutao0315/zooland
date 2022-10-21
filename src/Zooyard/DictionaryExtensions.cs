namespace Zooyard;

public static class DictionaryExtensions
{
    public static void PutAll<T, V>(this IDictionary<T, V> value, IDictionary<T, V>? other) 
    {
        if (other == null) 
        {
            return;
        }
        foreach (var item in other)
        {
            if (value.ContainsKey(item.Key))
            {
                value.Remove(item.Key);
            }
            value.Add(item.Key, item.Value);
        }
    }

    public static T GetValue<T>(this IDictionary<string, string> meta, string key, T defaultValue = default!) 
        where T : IConvertible
    {
        if (!meta.TryGetValue(key, out string? val) || string.IsNullOrWhiteSpace(val))
        {
            return defaultValue;
        }

        try
        {
            var b = val.ChangeType(typeof(T));
            if (b == null)
            {
                return defaultValue;
            }
            return (T)b;
        }
        catch
        {
            return defaultValue;
        }
    }
}
