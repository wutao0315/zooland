using Zooyard.Rpc;
using Microsoft.Extensions.Primitives;

namespace Zooyard.Configuration.ConfigProvider;

internal sealed class ConfigurationSnapshot : IRpcConfig
{
    public List<RouteConfig> Routes { get; internal set; } = [];
    public List<ServiceConfig> Services { get; internal set; } = [];

    IReadOnlyList<RouteConfig> IRpcConfig.Routes => Routes;
    IReadOnlyList<ServiceConfig> IRpcConfig.Services => Services;

    // This field is required.
    public IChangeToken ChangeToken { get; internal set; } = default!;
}
