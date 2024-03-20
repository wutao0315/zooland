using System.Diagnostics;

namespace Zooyard.Realtime;

internal sealed class DictionaryDebugView<TKey, TValue> where TKey : notnull
{
    private readonly IDictionary<TKey, TValue> _dict;

    public DictionaryDebugView(IDictionary<TKey, TValue> dictionary)
    {
        _dict = dictionary;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public DictionaryItemDebugView<TKey, TValue>[] Items
    {
        get
        {
            var keyValuePairs = new KeyValuePair<TKey, TValue>[_dict.Count];
            _dict.CopyTo(keyValuePairs, 0);
            var items = new DictionaryItemDebugView<TKey, TValue>[keyValuePairs.Length];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new DictionaryItemDebugView<TKey, TValue>(keyValuePairs[i]);
            }
            return items;
        }
    }
}

/// <summary>
/// Defines a key/value pair for displaying an item of a dictionary by a debugger.
/// </summary>
[DebuggerDisplay("{Value}", Name = "[{Key}]")]
internal readonly struct DictionaryItemDebugView<TKey, TValue>
{
    public DictionaryItemDebugView(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    public DictionaryItemDebugView(KeyValuePair<TKey, TValue> keyValue)
    {
        Key = keyValue.Key;
        Value = keyValue.Value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public TKey Key { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public TValue Value { get; }
}
