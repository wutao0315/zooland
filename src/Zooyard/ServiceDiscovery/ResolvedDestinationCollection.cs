using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Configuration;

namespace Zooyard.ServiceDiscovery;

public sealed class ResolvedDestinationCollection
{
    public ResolvedDestinationCollection(IReadOnlyDictionary<string, DestinationConfig> destinations, IChangeToken? changeToken)
    {
        Destinations = destinations;
        ChangeToken = changeToken;
    }

    /// <summary>
    /// Gets the map of destination names to destination configurations.
    /// </summary>
    public IReadOnlyDictionary<string, DestinationConfig> Destinations { get; init; }

    /// <summary>
    /// Gets the optional change token used to signal when this collection should be refreshed.
    /// </summary>
    public IChangeToken? ChangeToken { get; init; }
}
