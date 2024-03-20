namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Provides acccess to the request-scoped <see cref="IServiceProvider"/>.
/// </summary>
public interface IServiceProvidersFeature
{
    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> scoped to the current request.
    /// </summary>
    IServiceProvider RequestServices { get; set; }
}
