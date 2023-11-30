using Zooyard.Configuration;

namespace Zooyard.Model;

public sealed class ServiceModel(ServiceConfig config, HttpMessageInvoker httpClient)
{
    /// <summary>
    /// The config for this cluster.
    /// </summary>
    public ServiceConfig Config { get; } = config ?? throw new ArgumentNullException(nameof(config));

    /// <summary>
    /// An <see cref="HttpMessageInvoker"/> that used for proxying requests to an upstream server.
    /// </summary>
    public HttpMessageInvoker HttpClient { get; } = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    // We intentionally do not consider destination changes when updating the cluster Revision.
    // Revision is used to rebuild routing endpoints which should be unrelated to destinations,
    // and destinations are the most likely to change.
    internal bool HasConfigChanged(ServiceModel newModel)
    {
        return !Config.EqualsExcludingDestinations(newModel.Config) || newModel.HttpClient != HttpClient;
    }
}
