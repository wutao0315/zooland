using Zooyard.Configuration;

namespace Zooyard.ConfigurationMapper;

public sealed record ServiceOption
{
    public IReadOnlyDictionary<string, NamingOption> Services { get; init; } = new Dictionary<string, NamingOption>();
}
public sealed record NamingOption
{
    public float ProtectThreshold { get; init; }
    public string RouteType { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, NamingInstanceOption> Instances { get; init; } = new Dictionary<string, NamingInstanceOption>();
}
public sealed record NamingInstanceOption
{
    public bool Ephemeral { get; init; }
    public float Weight { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed record ZooyardServiceOption
{
    public IReadOnlyList<string> Contracts { get; init; } = new List<string>();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, ServiceConfig> Services { get; init; } = new Dictionary<string, ServiceConfig>();
}
