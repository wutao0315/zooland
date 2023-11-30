using Zooyard.Configuration;

namespace Zooyard.ServiceDiscovery;

public interface IInstanceResolver
{
    /// <summary>
    /// Resolves the provided destinations and returns resolved destinations.
    /// </summary>
    /// <param name="instances">The destinations to resolve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The resolved destinations and a change token used to indicate when resolution should be performed again.
    /// </returns>
    ValueTask<ResolvedInstanceCollection> ResolveInstancesAsync(
        IReadOnlyDictionary<string, InstanceConfig> instances,
        CancellationToken cancellationToken);
}
