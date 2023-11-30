using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zooyard.Configuration;

namespace ZooyardClient;

public static class ServiceExtensions
{
    //public const string ServiceDefault = "default";
    public static string YardKey { get; private set; } = "zooyard";

    /// <summary>
    /// Add Zoo service discovery support
    /// </summary>
    /// <param name="builder">builder</param>
    /// <param name="configuration">configuration</param>
    /// <param name="yardKey">yarp key</param>
    /// <param name="serviceKey">service key</param>
    /// <returns>IRpcBuilder</returns>
    public static IRpcBuilder AddServiceDiscovery(
        this IRpcBuilder builder, IConfiguration configuration, string yardKey, string serviceKey)
    {
        YardKey = yardKey;
        builder.Services.Configure<ZooyardServiceOption>(configuration.GetSection(yardKey));
        builder.Services.Configure<ServiceOption>(configuration.GetSection(serviceKey));
        builder.Services.AddSingleton<IYardConfigMapper, DefaultYardConfigMapper>();
        builder.Services.AddSingleton<IRpcConfigProvider, YardRpcConfigProvider>();

       return builder;
    }
    
}
