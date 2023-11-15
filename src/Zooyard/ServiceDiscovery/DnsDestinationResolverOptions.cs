using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.ServiceDiscovery;

public class DnsDestinationResolverOptions
{
    /// <summary>
    /// The period between requesting a refresh of a resolved name.
    /// </summary>
    /// <remarks>
    /// Defaults to 5 minutes.
    /// </remarks>
    public TimeSpan? RefreshPeriod { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The optional address family to query for.
    /// Use <see cref="AddressFamily.InterNetwork"/> for IPv4 addresses and <see cref="AddressFamily.InterNetworkV6"/> for IPv6 addresses.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="null"/> (any address).
    /// </remarks>
    public AddressFamily? AddressFamily { get; set; }
}
