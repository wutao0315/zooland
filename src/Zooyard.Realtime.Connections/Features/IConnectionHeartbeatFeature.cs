namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// A feature that represents the connection heartbeat.
/// </summary>
public interface IConnectionHeartbeatFeature
{
    /// <summary>
    /// Registers the given <paramref name="action"/> to be called with the associated <paramref name="state"/> on each heartbeat of the connection.
    /// </summary>
    /// <param name="action">The <see cref="Action{T}"/> to invoke.</param>
    /// <param name="state">The state for the <paramref name="action"/>.</param>
    void OnHeartbeat(Action<object> action, object state);
}