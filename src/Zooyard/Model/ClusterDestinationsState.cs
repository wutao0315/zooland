using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Model;

public sealed class ClusterDestinationsState
{
    public ClusterDestinationsState(
        IReadOnlyList<DestinationState> allDestinations,
        IReadOnlyList<DestinationState> availableDestinations)
    {
        AllDestinations = allDestinations ?? throw new ArgumentNullException(nameof(allDestinations));
        AvailableDestinations = availableDestinations ?? throw new ArgumentNullException(nameof(availableDestinations));
    }

    public IReadOnlyList<DestinationState> AllDestinations { get; }

    public IReadOnlyList<DestinationState> AvailableDestinations { get; }
}
