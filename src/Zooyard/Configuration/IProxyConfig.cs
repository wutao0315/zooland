using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;

namespace Zooyard.Configuration;

public interface IProxyConfig
{
    private static readonly ConditionalWeakTable<IProxyConfig, string> _revisionIdsTable = new();

    /// <summary>
    /// A unique identifier for this revision of the configuration.
    /// </summary>
    string RevisionId => _revisionIdsTable.GetValue(this, static _ => Guid.NewGuid().ToString());

    /// <summary>
    /// Routes matching requests to clusters.
    /// </summary>
    IReadOnlyList<RouteConfig> Routes { get; }

    /// <summary>
    /// Cluster information for where to proxy requests to.
    /// </summary>
    IReadOnlyList<ClusterConfig> Clusters { get; }

    /// <summary>
    /// A notification that triggers when this snapshot expires.
    /// </summary>
    IChangeToken ChangeToken { get; }
}
