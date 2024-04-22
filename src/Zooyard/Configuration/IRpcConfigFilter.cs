namespace Zooyard.Configuration;

public interface IRpcConfigFilter
{
    /// <summary>
    /// Allows modification of a service configuration.
    /// </summary>
    /// <param name="service">The <see cref="ServiceConfig"/> instance to configure.</param>
    /// <param name="cancel"></param>
    ValueTask<ServiceConfig> ConfigureServiceAsync(ServiceConfig service, CancellationToken cancel);
    /// <summary>
    /// Allows modification of a public configuration.
    /// </summary>
    /// <param name="route">The <see cref="RouteConfig"/> instance to configure.</param>
    /// <param name="cancel"></param>
    ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, CancellationToken cancel);
}
