using Microsoft.Extensions.Primitives;

namespace Zooyard.Configuration.ConfigProvider;

internal sealed class ConfigurationSnapshot : IRpcConfig
{
    public IReadOnlyList<string> Contracts { get; internal set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Metadata { get; internal set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, ServiceConfig> Services { get; internal set; } = new Dictionary<string, ServiceConfig>();
    IReadOnlyDictionary<string, string> IRpcConfig.Metadata => Metadata;
    IReadOnlyDictionary<string, ServiceConfig> IRpcConfig.Services  => Services;
    IReadOnlyList<string> IRpcConfig.Contracts => Contracts;
    // This field is required.
    public IChangeToken ChangeToken { get; internal set; } = default!;
}
