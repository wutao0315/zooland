using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Configuration;

namespace Zooyard.Model;

public sealed class RouteModel
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public RouteModel(
        RouteConfig config,
        ClusterState? cluster)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Cluster = cluster;
    }

    // May not be populated if the cluster config is missing. https://github.com/microsoft/reverse-proxy/issues/797
    /// <summary>
    /// The <see cref="ClusterState"/> instance associated with this route.
    /// </summary>
    public ClusterState? Cluster { get; }

    /// <summary>
    /// The configuration data used to build this route.
    /// </summary>
    public RouteConfig Config { get; }

    internal bool HasConfigChanged(RouteConfig newConfig, ClusterState? cluster, int? routeRevision)
    {
        return Cluster != cluster || routeRevision != cluster?.Revision || !Config.Equals(newConfig);
    }
}
