using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;

namespace Zooyard.Configuration;

public interface IRpcConfig
{
    private static readonly ConditionalWeakTable<IRpcConfig, string> _revisionIdsTable = [];

    /// <summary>
    /// A unique identifier for this revision of the configuration.
    /// </summary>
    string RevisionId => _revisionIdsTable.GetValue(this, static _ => Guid.NewGuid().ToString());

    /// <summary>
    /// Routes matching requests to clusters.
    /// </summary>
    IReadOnlyList<RouteConfig>? Routes { get; }

    /// <summary>
    /// Cluster information for where to proxy requests to.
    /// </summary>
    IReadOnlyList<ServiceConfig>? Services { get; }

    /// <summary>
    /// A notification that triggers when this snapshot expires.
    /// </summary>
    IChangeToken ChangeToken { get; }
}
