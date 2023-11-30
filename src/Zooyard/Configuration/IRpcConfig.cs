using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;

namespace Zooyard.Configuration;

public interface IRpcConfig
{
    private static readonly ConditionalWeakTable<IRpcConfig, string> _revisionIdsTable = new();

    /// <summary>
    /// A unique identifier for this revision of the configuration.
    /// </summary>
    string RevisionId => _revisionIdsTable.GetValue(this, static _ => Guid.NewGuid().ToString());
    IReadOnlyList<string> Contracts { get; }
    IReadOnlyDictionary<string, string> Metadata { get;}
    IReadOnlyDictionary<string, ServiceConfig> Services { get; }

    /// <summary>
    /// A notification that triggers when this snapshot expires.
    /// </summary>
    IChangeToken ChangeToken { get; }
}
