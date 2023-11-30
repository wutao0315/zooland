﻿using Zooyard.Model;

namespace Zooyard.Utils;

internal sealed class ParsedMetadataEntry<T>
{
    private readonly Parser _parser;
    private readonly string _metadataName;
    private readonly ServiceState _service;
    // Use a volatile field of a reference Tuple<T1, T2> type to ensure atomicity during concurrent access.
    private volatile Tuple<string?, T>? _value;

    public delegate bool Parser(string stringValue, out T parsedValue);

    public ParsedMetadataEntry(Parser parser, ServiceState cluster, string metadataName)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _service = cluster ?? throw new ArgumentNullException(nameof(cluster));
        _metadataName = metadataName ?? throw new ArgumentNullException(nameof(metadataName));
    }

    public T GetParsedOrDefault(T defaultValue)
    {
        var currentValue = _value;
        if (_service.Model.Config.Metadata is not null && _service.Model.Config.Metadata.TryGetValue(_metadataName, out var stringValue))
        {
            if (currentValue is null || currentValue.Item1 != stringValue)
            {
                _value = Tuple.Create<string?, T>(stringValue, _parser(stringValue, out var parsedValue) ? parsedValue : defaultValue);
            }
        }
        else if (currentValue is null || currentValue.Item1 is not null)
        {
            _value = Tuple.Create<string?, T>(null, defaultValue);
        }

        return _value!.Item2;
    }
}
