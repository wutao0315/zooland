using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Zooyard.Realtime.Connections;
using Zooyard.Realtime.Connections.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ConnectionsDependencyInjectionExtensions
{
    /// <summary>
    /// Adds required services for ASP.NET Core Connection Handlers to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddRpcConnections(this IServiceCollection services)
    {
        services.AddRouting();
        //services.AddAuthorization();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ConnectionOptions>, ConnectionOptionsSetup>());
        services.TryAddSingleton<RpcConnectionDispatcher>();
        services.TryAddSingleton<RpcConnectionManager>();
        services.TryAddSingleton<RpcConnectionsMetrics>();
        return services;
    }

    /// <summary>
    /// Adds required services for ASP.NET Core Connection Handlers to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="options">A callback to configure  <see cref="ConnectionOptions" /></param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddConnections(this IServiceCollection services, Action<ConnectionOptions> options)
    {
        return services.Configure(options)
            .AddRpcConnections();
    }
}
