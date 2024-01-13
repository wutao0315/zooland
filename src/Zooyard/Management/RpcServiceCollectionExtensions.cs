using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Zooyard;
using Zooyard.Configuration;
using Zooyard.Configuration.ConfigProvider;
using Zooyard.DataAnnotations;
using Zooyard.Management;
using Zooyard.Rpc.Route.State;
using Zooyard.Rpc;
using Zooyard.ServiceDiscovery;

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
    public static IRpcBuilder AddRpc(this IServiceCollection services)
    {
        var builder = new RpcBuilder(services);
        builder
            .AddConfigBuilder()
            .AddRuntimeStateManagers()
            .AddConfigManager()
            .AddInstanceResolver();

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

        builder.Services.TryAddSingleton<IRpcConfigProvider>(sp =>
        {
            // This is required because we're capturing the configuration via a closure
            return new ConfigurationConfigProvider(sp.GetRequiredService<ILogger<ConfigurationConfigProvider>>(), config);
        });

        return builder;
    }

    public static IRpcBuilder AddContracts(this IRpcBuilder builder, params Type[] types)
    {
        builder.Services.AddContracts(types);
        return builder;
    }

    public static void AddContracts(this IServiceCollection service, params Type[] types) 
    {
        service.TryAddSingleton<IZooyardPools>((serviceProvder) =>
        {
            var loggerfactory = serviceProvder.GetRequiredService<ILoggerFactory>();
            //var option = serviceProvder.GetRequiredService<IOptionsMonitor<ZooyardOption>>();
            var aa = serviceProvder.GetRequiredService<IRpcStateLookup>();

            var clientPools = new Dictionary<string, IClientPool>();
            foreach (var serviceType in types)
            {
                var zooyard = serviceType.GetCustomAttribute<ZooyardAttribute>();
                if (zooyard == null)
                {
                    throw new RpcException($"{nameof(ZooyardAttribute)} is not exists");
                }

                var poolType = Type.GetType(zooyard.TypeName)!;

                var pool = (IClientPool)serviceProvder.GetRequiredService(poolType);
                pool.ServiceName = zooyard.ServiceName;
                pool.ProxyType = zooyard.ProxyType;
                clientPools.Add(serviceType.FullName!, pool);
            }

            var loadBalances = serviceProvder.GetServices<ILoadBalance>();
            var clusters = serviceProvder.GetServices<ICluster>();
            var caches = serviceProvder.GetServices<ICache>();
            var routeFactories = serviceProvder.GetServices<IStateRouterFactory>();
            var zooyardPools = new ZooyardPools(loggerfactory, clientPools, loadBalances, clusters, caches, routeFactories, aa);
            return zooyardPools;
        });


        foreach (var serviceType in types)
        {
            var genericType = typeof(ZooyardFactory<>);
            var factoryType = genericType.MakeGenericType(new[] { serviceType });
            service.TryAddSingleton(factoryType, (serviceProvder) =>
            {
                var loggerfactory = serviceProvder.GetRequiredService<ILoggerFactory>();
                var pools = serviceProvder.GetRequiredService<IZooyardPools>();
                var constructor = factoryType.GetConstructor(new[] { typeof(ILoggerFactory), typeof(IZooyardPools), typeof(string), typeof(string), typeof(string) });
                if (constructor == null)
                {
                    throw new RpcException($"{nameof(constructor)} is not exists");
                }
                var zooyard = serviceType.GetCustomAttribute<ZooyardAttribute>();
                if (zooyard == null)
                {
                    throw new RpcException($"{nameof(ZooyardAttribute)} is not exists");
                }
                var zooyardFactory = constructor.Invoke(new object[] { loggerfactory, pools, zooyard.ServiceName, zooyard.Version, zooyard.Url });
                return zooyardFactory;
            });


            service.TryAddTransient(serviceType, (serviceProvder) =>
            {
                var factory = serviceProvder.GetRequiredService(factoryType);
                var createYardMethod = factoryType.GetMethod("CreateYard");
                if (createYardMethod == null)
                {
                    throw new RpcException($"{nameof(createYardMethod)} is not exists");
                }
                var result = createYardMethod.Invoke(factory, null);
                if (result == null)
                {
                    throw new RpcException($"{nameof(createYardMethod)} is not null");
                }
                return result;
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


    /// <summary>
    /// Provides a <see cref="IInstanceResolver"/> implementation which uses <see cref="System.Net.Dns"/> to resolve destinations.
    /// </summary>
    public static IRpcBuilder AddDnsInstanceResolver(this IRpcBuilder builder, Action<DnsInstanceResolverOptions>? configureOptions = null)
    {
        builder.Services.TryAddSingleton<IInstanceResolver, DnsInstanceResolver>();
        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }
}