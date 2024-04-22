namespace Zooyard.Configuration;

/// <summary>
/// Provides methods to validate routes and clusters.
/// </summary>
public interface IConfigValidator
{
    /// <summary>
    /// Validates a public and returns all errors
    /// </summary>
    ValueTask<IList<Exception>> ValidateRouteAsync(RouteConfig route);
    /// <summary>
    /// Validates a service and returns all errors.
    /// </summary>
    ValueTask<IList<Exception>> ValidateServiceAsync(ServiceConfig service);
}
