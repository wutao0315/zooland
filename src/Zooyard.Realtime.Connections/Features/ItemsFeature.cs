using Microsoft.AspNetCore.Http.Internal;

namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Default implementation for <see cref="IItemsFeature"/>.
/// </summary>
public class ItemsFeature : IItemsFeature
{
    /// <summary>
    /// Initializes a new instance of <see cref="ItemsFeature"/>.
    /// </summary>
    public ItemsFeature()
    {
        Items = new ItemsDictionary();
    }

    /// <inheritdoc />
    public IDictionary<object, object?> Items { get; set; }
}
