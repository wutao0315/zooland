﻿using Zooyard.Realtime.Internal;

namespace Zooyard.Realtime.Features;

/// <summary>
/// Extension methods for getting feature from <see cref="IFeatureCollection"/>
/// </summary>
public static class FeatureCollectionExtensions
{
    /// <summary>
    /// Retrives the requested feature from the collection.
    /// Throws an <see cref="InvalidOperationException"/> if the feature is not present.
    /// </summary>
    /// <param name="featureCollection">The <see cref="IFeatureCollection"/>.</param>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <returns>The requested feature.</returns>
    public static TFeature GetRequiredFeature<TFeature>(this IFeatureCollection featureCollection)
        where TFeature : notnull
    {
        ArgumentNullThrowHelper.ThrowIfNull(featureCollection);

        return featureCollection.Get<TFeature>() ?? throw new InvalidOperationException($"Feature '{typeof(TFeature)}' is not present.");
    }

    /// <summary>
    /// Retrives the requested feature from the collection.
    /// Throws an <see cref="InvalidOperationException"/> if the feature is not present.
    /// </summary>
    /// <param name="featureCollection">feature collection</param>
    /// <param name="key">The feature key.</param>
    /// <returns>The requested feature.</returns>
    public static object GetRequiredFeature(this IFeatureCollection featureCollection, Type key)
    {
        ArgumentNullThrowHelper.ThrowIfNull(featureCollection);
        ArgumentNullThrowHelper.ThrowIfNull(key);

        return featureCollection[key] ?? throw new InvalidOperationException($"Feature '{key}' is not present.");
    }
}
