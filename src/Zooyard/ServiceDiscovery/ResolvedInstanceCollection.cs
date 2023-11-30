using Microsoft.Extensions.Primitives;
using Zooyard.Configuration;

namespace Zooyard.ServiceDiscovery;

public sealed class ResolvedInstanceCollection
{
    public ResolvedInstanceCollection(IReadOnlyDictionary<string, InstanceConfig> instances, IChangeToken? changeToken)
    {
        Instances = instances;
        ChangeToken = changeToken;
    }

    /// <summary>
    /// Gets the map of destination names to destination configurations.
    /// </summary>
    public IReadOnlyDictionary<string, InstanceConfig> Instances { get; init; }

    /// <summary>
    /// Gets the optional change token used to signal when this collection should be refreshed.
    /// </summary>
    public IChangeToken? ChangeToken { get; init; }
}
