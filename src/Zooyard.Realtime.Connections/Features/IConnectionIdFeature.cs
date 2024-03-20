namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// The unique identifier for a given connection.
/// </summary>
public interface IConnectionIdFeature
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    string ConnectionId { get; set; }
}
