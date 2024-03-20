using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Provides access to server websocket features.
/// </summary>
public interface IHttpWebSocketFeature
{
    /// <summary>
    /// Indicates if this is a WebSocket upgrade request.
    /// </summary>
    bool IsWebSocketRequest { get; }

    /// <summary>
    /// Attempts to upgrade the request to a <see cref="WebSocket"/>. Check <see cref="IsWebSocketRequest"/>
    /// before invoking this.
    /// </summary>
    /// <param name="context">The <see cref="WebSocketAcceptContext"/>.</param>
    /// <returns>A <see cref="WebSocket"/>.</returns>
    Task<WebSocket> AcceptAsync(WebSocketAcceptContext context);
}