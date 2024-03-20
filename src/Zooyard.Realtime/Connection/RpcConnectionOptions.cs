namespace Zooyard.Realtime.Connection;

/// <summary>
/// Configures options for the <see cref="RpcConnection" />.
/// </summary>
public sealed record RpcConnectionOptions
{
    /// <summary>
    /// Configures ServerTimeout for the <see cref="RpcConnection" />.
    /// </summary>
    public TimeSpan ServerTimeout { get; set; } = RpcConnection.DefaultServerTimeout;

    /// <summary>
    /// Configures KeepAliveInterval for the <see cref="RpcConnection" />.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = RpcConnection.DefaultKeepAliveInterval;

    /// <summary>
    /// Amount of serialized messages in bytes we'll buffer when using Stateful Reconnect until applying backpressure to sends from the client.
    /// </summary>
    /// <remarks>Defaults to 100,000 bytes.</remarks>
    public long StatefulReconnectBufferSize { get; set; } = RpcConnection.DefaultStatefulReconnectBufferSize;

}
