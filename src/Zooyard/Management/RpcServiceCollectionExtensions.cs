using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Zooyard;
using Zooyard.Attributes;
using Zooyard.Configuration;
using Zooyard.Configuration.ConfigProvider;
using Zooyard.Management;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;
//using Zooyard.ServiceDiscovery;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>
/// used to register the ReverseProxy's components.
/// </summary>
public static class RpcServiceCollectionExtensions
{
    /// <summary>
    /// Adds Rpc's services to Dependency Injection.
    /// </summary>
    public static IRpcBuilder AddRpcDefault(this IServiceCollection services)
    {
        var builder = new RpcBuilder(services);
        builder
            .AddConfigBuilder(new Dictionary<string, Type> { [ZooyardHttpAttribute.TYPENAME] = typeof(ResponseDataResult<>) })
            .AddRuntimeStateManagers()
            .AddConfigManager()
            //.AddInstanceResolver()
            .AddInterceptor<ResponseRpcInterceptor>();

        return builder;
    }

    /// <summary>
    /// Adds Rpc's services to Dependency Injection.
    /// </summary>
    public static IRpcBuilder AddRpc(this IServiceCollection services,Dictionary<string, Type> baseReturnTypes, Type? baseInterceptor = null)
    {
        
        var builder = new RpcBuilder(services);
        builder
            .AddConfigBuilder(baseReturnTypes)
            .AddRuntimeStateManagers()
            .AddConfigManager()
            //.AddInstanceResolver()
            ;

        if (baseInterceptor != null) 
        {
            builder.AddInterceptor(baseInterceptor);
        }

        return builder;
    }

    /// <summary>
    /// Loads routes and endpoints from config.
    /// </summary>
    public static IRpcBuilder LoadFromConfig(this IRpcBuilder builder, IConfiguration config)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        builder.Services.AddSingleton<IRpcConfigProvider>(sp =>
        {
            // This is required because we're capturing the configuration via a closure
            return new ConfigurationConfigProvider(sp.GetRequiredService<ILogger<ConfigurationConfigProvider>>(), config);
        });

        return builder;
    }

    public static IRpcBuilder AddInterceptor<T>(this IRpcBuilder builder)
        where T: class, IInterceptor
    {
        builder.AddInterceptor(typeof(T));
        return builder;
    }

    public static IRpcBuilder AddInterceptor(this IRpcBuilder builder, Type type)
    {
        builder.Services.TryAddSingleton(typeof(IInterceptor), type);
        return builder;
    }

    public static IRpcBuilder AddContract<T>(this IRpcBuilder builder)
    {
        builder.AddContracts(typeof(T));
        return builder;
    }

    public static void AddContracts(this IRpcBuilder builder, params Type[] types) 
    {
        foreach (var serviceType in types)
        {
            builder.Services.AddSingleton<IClientPool>((s) => 
            {
                var zooyard = serviceType.GetCustomAttribute<ZooyardAttribute>();
                if (zooyard == null)
                {
                    throw new RpcException($"{nameof(ZooyardAttribute)} is not exists");
                }

                var poolType = Type.GetType(zooyard.TypeName)!;

                var pool = (AbstractClientPool)s.GetRequiredService(poolType);
                pool.ServiceName = zooyard.ServiceName;
                pool.ProxyType = zooyard.ProxyType;
                pool.Name = serviceType.FullName!;
                return pool;
            });
        }
    }

    /// <summary>
    /// Registers a singleton IProxyConfigFilter service. Multiple filters are allowed and they will be run in registration order.
    /// </summary>
    /// <typeparam name="TService">A class that implements IProxyConfigFilter.</typeparam>
    public static IRpcBuilder AddConfigFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IRpcBuilder builder) where TService : class, IRpcConfigFilter
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IRpcConfigFilter, TService>());
        return builder;
    }


    ///// <summary>
    ///// Provides a <see cref="IInstanceResolver"/> implementation which uses <see cref="System.Net.Dns"/> to resolve destinations.
    ///// </summary>
    //public static IRpcBuilder AddDnsInstanceResolver(this IRpcBuilder builder, Action<DnsInstanceResolverOptions>? configureOptions = null)
    //{
    //    builder.Services.TryAddSingleton<IInstanceResolver, DnsInstanceResolver>();
    //    if (configureOptions is not null)
    //    {
    //        builder.Services.Configure(configureOptions);
    //    }

    //    return builder;
    //}
}