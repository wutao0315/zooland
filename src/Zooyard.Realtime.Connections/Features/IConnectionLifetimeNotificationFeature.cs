namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Enables graceful termination of the connection.
/// </summary>
public interface IConnectionLifetimeNotificationFeature
{
    /// <summary>
    /// Gets or set an <see cref="CancellationToken"/> that will be triggered when closing the connection has been requested.
    /// </summary>
    CancellationToken ConnectionClosedRequested { get; set; }

    /// <summary>
    /// Requests the connection to be closed.
    /// </summary>
    void RequestClose();
}

