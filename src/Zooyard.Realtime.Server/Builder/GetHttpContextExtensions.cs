
using Zooyard.Realtime.Connections;
using Zooyard.Realtime.Connections.Features;

namespace Zooyard.Realtime.Server;

/// <summary>
/// Extension methods for accessing <see cref="HttpContext"/> from a hub context.
/// </summary>
public static class GetHttpContextExtensions
{
    /// <summary>
    /// Gets <see cref="HttpContext"/> from the specified connection, or <c>null</c> if the connection is not associated with an HTTP request.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <returns>The <see cref="HttpContext"/> for the connection, or <c>null</c> if the connection is not associated with an HTTP request.</returns>
    public static HttpContext? GetHttpContext(this HubCallerContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
    }

    /// <summary>
    /// Gets <see cref="HttpContext"/> from the specified connection, or <c>null</c> if the connection is not associated with an HTTP request.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <returns>The <see cref="HttpContext"/> for the connection, or <c>null</c> if the connection is not associated with an HTTP request.</returns>
    public static HttpContext? GetHttpContext(this HubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
    }
}
