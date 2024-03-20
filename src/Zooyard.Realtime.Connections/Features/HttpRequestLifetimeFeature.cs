namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Default implementation for <see cref="IHttpRequestLifetimeFeature"/>.
/// </summary>
public class HttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
{
    /// <inheritdoc />
    public CancellationToken RequestAborted { get; set; }

    /// <inheritdoc />
    public void Abort()
    {
    }
}
