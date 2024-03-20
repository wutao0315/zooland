using System.Net;

namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Default implementation for <see cref="IHttpConnectionFeature"/>.
/// </summary>
public class HttpConnectionFeature : IHttpConnectionFeature
{
    /// <inheritdoc />
    public string ConnectionId { get; set; } = default!;

    /// <inheritdoc />
    public IPAddress? LocalIpAddress { get; set; }

    /// <inheritdoc />
    public int LocalPort { get; set; }

    /// <inheritdoc />
    public IPAddress? RemoteIpAddress { get; set; }

    /// <inheritdoc />
    public int RemotePort { get; set; }
}
