using Zooyard.Configuration;

namespace Zooyard.ServiceDiscovery;

internal sealed class NoOpInstanceResolver : IInstanceResolver
{
    public ValueTask<ResolvedInstanceCollection> ResolveInstancesAsync(IReadOnlyDictionary<string, InstanceConfig> instances, CancellationToken cancellationToken)
        => new(new ResolvedInstanceCollection(instances, changeToken: null));
}
