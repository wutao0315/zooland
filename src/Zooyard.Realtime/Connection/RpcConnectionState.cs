namespace Zooyard.Realtime.Connection;

/// <summary>
/// Describes the current state of the <see cref="RpcConnection"/> to the server.
/// </summary>
public enum RpcConnectionState
{
    /// <summary>
    /// The hub connection is disconnected.
    /// </summary>
    Disconnected,
    /// <summary>
    /// The hub connection is connected.
    /// </summary>
    Connected,
    /// <summary>
    /// The hub connection is connecting.
    /// </summary>
    Connecting,
    /// <summary>
    /// The hub connection is reconnecting.
    /// </summary>
    Reconnecting,
}