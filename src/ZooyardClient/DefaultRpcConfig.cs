using Microsoft.Extensions.Primitives;
using Zooyard.Configuration;

namespace Zooyard.ConfigurationMapper;

internal class DefaultRpcConfig(IReadOnlyList<string> contracts, IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, ServiceConfig> services)
    : IRpcConfig
{
    public IReadOnlyList<string> Contracts => contracts;
    public IReadOnlyDictionary<string, string> Metadata => metadata;
    //public IReadOnlyDictionary<string, ServiceConfig> Services => services;
    public IChangeToken ChangeToken { get; internal set; } = default!;

    public IReadOnlyList<RouteConfig> Routes { get; }

    public IReadOnlyList<ServiceConfig> Services { get; }
}

