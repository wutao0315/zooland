namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// A bag of items associated with a given connection.
/// </summary>
public interface IConnectionItemsFeature
{
    /// <summary>
    /// Gets or sets the items associated with the connection.
    /// </summary>
    IDictionary<object, object?> Items { get; set; }
}

