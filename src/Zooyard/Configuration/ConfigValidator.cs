using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using Zooyard.Utils;

namespace Zooyard.Configuration;

internal sealed class ConfigValidator : IConfigValidator
{
    private static readonly HashSet<string> _validMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "HEAD", "OPTIONS", "GET", "PUT", "POST", "PATCH", "DELETE", "TRACE",
};

    private readonly ILogger _logger;


    public ConfigValidator(
        ILogger<ConfigValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    //    // Note this performs all validation steps without short circuiting in order to report all possible errors.
    //    public async ValueTask<IList<Exception>> ValidateRouteAsync(RouteConfig route)
    //    {
    //        _ = route ?? throw new ArgumentNullException(nameof(route));
    //        var errors = new List<Exception>();

    //        if (string.IsNullOrEmpty(route.RouteId))
    //        {
    //            errors.Add(new ArgumentException("Missing Route Id."));
    //        }

    //        await Task.CompletedTask;

    ////        errors.AddRange(_transformBuilder.ValidateRoute(route));
    ////        await ValidateAuthorizationPolicyAsync(errors, route.AuthorizationPolicy, route.RouteId);
    ////#if NET7_0_OR_GREATER
    ////        await ValidateRateLimiterPolicyAsync(errors, route.RateLimiterPolicy, route.RouteId);
    ////#endif
    ////        await ValidateCorsPolicyAsync(errors, route.CorsPolicy, route.RouteId);

    ////        if (route.Match is null)
    ////        {
    ////            errors.Add(new ArgumentException($"Route '{route.RouteId}' did not set any match criteria, it requires Hosts or Path specified. Set the Path to '/{{**catchall}}' to match all requests."));
    ////            return errors;
    ////        }

    ////        if ((route.Match.Hosts is null || !route.Match.Hosts.Any(host => !string.IsNullOrEmpty(host))) && string.IsNullOrEmpty(route.Match.Path))
    ////        {
    ////            errors.Add(new ArgumentException($"Route '{route.RouteId}' requires Hosts or Path specified. Set the Path to '/{{**catchall}}' to match all requests."));
    ////        }

    //        //ValidateHost(errors, route.Match.Hosts, route.RouteId);
    //        //ValidatePath(errors, route.Match.Path, route.RouteId);
    //        //ValidateMethods(errors, route.Match.Methods, route.RouteId);
    //        //ValidateHeaders(errors, route.Match.Headers, route.RouteId);
    //        //ValidateQueryParameters(errors, route.Match.QueryParameters, route.RouteId);

    //        return errors;
    //    }

    //// Note this performs all validation steps without short circuiting in order to report all possible errors.
    //public ValueTask<IList<Exception>> ValidateClusterAsync(ClusterConfig cluster)
    //{
    //    _ = cluster ?? throw new ArgumentNullException(nameof(cluster));
    //    var errors = new List<Exception>();

    //    if (string.IsNullOrEmpty(cluster.ClusterId))
    //    {
    //        errors.Add(new ArgumentException("Missing Cluster Id."));
    //    }

    //    //errors.AddRange(_transformBuilder.ValidateCluster(cluster));
    //    //ValidateDestinations(errors, cluster);
    //    //ValidateLoadBalancing(errors, cluster);
    //    //ValidateSessionAffinity(errors, cluster);
    //    //ValidateProxyHttpClient(errors, cluster);
    //    //ValidateProxyHttpRequest(errors, cluster);
    //    //ValidateHealthChecks(errors, cluster);

    //    return new ValueTask<IList<Exception>>(errors);
    //}

    // Note this performs all validation steps without short circuiting in order to report all possible errors.
    public ValueTask<IList<Exception>> ValidateServiceAsync(ServiceConfig service)
    {
        _ = service ?? throw new ArgumentNullException(nameof(service));
        var errors = new List<Exception>();

        if (string.IsNullOrEmpty(service.ServiceName))
        {
            errors.Add(new ArgumentException("Missing Service Name."));
        }

        //errors.AddRange(_transformBuilder.ValidateCluster(cluster));
        //ValidateDestinations(errors, cluster);
        //ValidateLoadBalancing(errors, cluster);
        //ValidateSessionAffinity(errors, cluster);
        //ValidateProxyHttpClient(errors, cluster);
        //ValidateProxyHttpRequest(errors, cluster);
        //ValidateHealthChecks(errors, cluster);

        return new ValueTask<IList<Exception>>(errors);
    }

    private static void ValidateHost(IList<Exception> errors, IReadOnlyList<string>? hosts, string routeId)
    {
        // Host is optional when Path is specified
        if (hosts is null || hosts.Count == 0)
        {
            return;
        }

        foreach (var host in hosts)
        {
            if (string.IsNullOrEmpty(host))
            {
                errors.Add(new ArgumentException($"Empty host name has been set for route '{routeId}'."));
            }
            else if (host.Contains("xn--", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ArgumentException($"Punycode host name '{host}' has been set for route '{routeId}'. Use the unicode host name instead."));
            }
        }
    }


    //private void ValidateHealthChecks(IList<Exception> errors, ClusterConfig cluster)
    //{
    //    var availableDestinationsPolicy = cluster.HealthCheck?.AvailableDestinationsPolicy;
    //    if (string.IsNullOrEmpty(availableDestinationsPolicy))
    //    {
    //        // The default.
    //        availableDestinationsPolicy = HealthCheckConstants.AvailableDestinations.HealthyOrPanic;
    //    }

    //    if (!_availableDestinationsPolicies.ContainsKey(availableDestinationsPolicy))
    //    {
    //        errors.Add(new ArgumentException($"No matching {nameof(IAvailableDestinationsPolicy)} found for the available destinations policy '{availableDestinationsPolicy}' set on the cluster.'{cluster.ClusterId}'."));
    //    }

    //    ValidateActiveHealthCheck(errors, cluster);
    //    ValidatePassiveHealthCheck(errors, cluster);
    //}

    //private void ValidateActiveHealthCheck(IList<Exception> errors, ClusterConfig cluster)
    //{
    //    if (!(cluster.HealthCheck?.Active?.Enabled ?? false))
    //    {
    //        // Active health check is disabled
    //        return;
    //    }

    //    var activeOptions = cluster.HealthCheck.Active;
    //    var policy = activeOptions.Policy;
    //    if (string.IsNullOrEmpty(policy))
    //    {
    //        // default policy
    //        policy = HealthCheckConstants.ActivePolicy.ConsecutiveFailures;
    //    }
    //    if (!_activeHealthCheckPolicies.ContainsKey(policy))
    //    {
    //        errors.Add(new ArgumentException($"No matching {nameof(IActiveHealthCheckPolicy)} found for the active health check policy name '{policy}' set on the cluster '{cluster.ClusterId}'."));
    //    }

    //    if (activeOptions.Interval is not null && activeOptions.Interval <= TimeSpan.Zero)
    //    {
    //        errors.Add(new ArgumentException($"Destination probing interval set on the cluster '{cluster.ClusterId}' must be positive."));
    //    }

    //    if (activeOptions.Timeout is not null && activeOptions.Timeout <= TimeSpan.Zero)
    //    {
    //        errors.Add(new ArgumentException($"Destination probing timeout set on the cluster '{cluster.ClusterId}' must be positive."));
    //    }
    //}

    //private void ValidatePassiveHealthCheck(IList<Exception> errors, ClusterConfig cluster)
    //{
    //    if (!(cluster.HealthCheck?.Passive?.Enabled ?? false))
    //    {
    //        // Passive health check is disabled
    //        return;
    //    }

    //    var passiveOptions = cluster.HealthCheck.Passive;
    //    var policy = passiveOptions.Policy;
    //    if (string.IsNullOrEmpty(policy))
    //    {
    //        // default policy
    //        policy = HealthCheckConstants.PassivePolicy.TransportFailureRate;
    //    }
    //    if (!_passiveHealthCheckPolicies.ContainsKey(policy))
    //    {
    //        errors.Add(new ArgumentException($"No matching {nameof(IPassiveHealthCheckPolicy)} found for the passive health check policy name '{policy}' set on the cluster '{cluster.ClusterId}'."));
    //    }

    //    if (passiveOptions.ReactivationPeriod is not null && passiveOptions.ReactivationPeriod <= TimeSpan.Zero)
    //    {
    //        errors.Add(new ArgumentException($"Unhealthy destination reactivation period set on the cluster '{cluster.ClusterId}' must be positive."));
    //    }
    //}

    private static class Log
    {
        private static readonly Action<ILogger, Exception?> _http10RequestVersionDetected = LoggerMessage.Define(
            LogLevel.Warning,
            EventIds.Http10RequestVersionDetected,
            "The HttpRequest version is set to 1.0 which can result in poor performance and port exhaustion. Use 1.1, 2, or 3 instead.");

        public static void Http10Version(ILogger logger)
        {
            _http10RequestVersionDetected(logger, null);
        }
    }
}
