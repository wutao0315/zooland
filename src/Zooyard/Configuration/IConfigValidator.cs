namespace Zooyard.Configuration;

/// <summary>
/// Provides methods to validate routes and clusters.
/// </summary>
public interface IConfigValidator
{
    ///// <summary>
    ///// Validates a route and returns all errors
    ///// </summary>
    //ValueTask<IList<Exception>> ValidateRouteAsync(RouteConfig route);

    ///// <summary>
    ///// Validates a cluster and returns all errors.
    ///// </summary>
    //ValueTask<IList<Exception>> ValidateClusterAsync(ClusterConfig cluster);

    ValueTask<IList<Exception>> ValidateServiceAsync(ServiceConfig service);

}
