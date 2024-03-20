namespace Zooyard.WebSocketsImpl.Connections;

/// <summary>
/// Used to make a connection to an SignalR using a WebSocket-based transport.
/// </summary>
public sealed class WebSocketConnectionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnectionContext"/> class.
    /// </summary>
    /// <param name="uri">The URL to connect to.</param>
    /// <param name="options">The connection options to use.</param>
    public WebSocketConnectionContext(Uri uri, WebSocketConnectionOptions options)
    {
        Uri = uri;
        Options = options;
    }

    /// <summary>
    /// Gets the URL to connect to.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Gets the connection options to use.
    /// </summary>
    public WebSocketConnectionOptions Options { get; }
}
