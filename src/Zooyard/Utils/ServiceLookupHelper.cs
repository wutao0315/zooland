﻿using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Zooyard.Utils;

internal static class ServiceLookupHelper
{
    public static FrozenDictionary<string, T> ToDictionaryByUniqueId<T>(this IEnumerable<T> services, Func<T, string> idSelector)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in services)
        {
            if (!result.TryAdd(idSelector(service), service))
            {
                throw new ArgumentException($"More than one {typeof(T)} found with the same identifier.", nameof(services));
            }
        }

        return result.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static T GetRequiredServiceById<T>(this FrozenDictionary<string, T> services, string? id, string defaultId)
    {
        var lookup = id;
        if (string.IsNullOrEmpty(lookup))
        {
            lookup = defaultId;
        }

        if (!services.TryGetValue(lookup, out var result))
        {
            throw new ArgumentException($"No {typeof(T)} was found for the id '{lookup}'.", nameof(id));
        }
        return result;
    }
}
