using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Configuration;

namespace Zooyard.ServiceDiscovery;

internal sealed class NoOpDestinationResolver : IDestinationResolver
{
    public ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(IReadOnlyDictionary<string, DestinationConfig> destinations, CancellationToken cancellationToken)
        => new(new ResolvedDestinationCollection(destinations, changeToken: null));
}
