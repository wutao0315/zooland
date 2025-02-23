using Zooyard.Configuration;
using Zooyard.Configuration.RouteValidators;
using Zooyard.Configuration.ServiceValidators;
using Zooyard.Rpc;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;
using Zooyard.Rpc.Merger;
using Zooyard.Rpc.Route.Condition;
using Zooyard.Rpc.Route.Condition.Config;
using Zooyard.Rpc.Route.File;
using Zooyard.Rpc.Route.Mock;
using Zooyard.Rpc.Route.None;
using Zooyard.Rpc.Route.Script;
using Zooyard.Rpc.Route.State;
using Zooyard.Rpc.Route.Tag;
using Zooyard.ServiceDiscovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Zooyard.Management;

internal static class IRpcBuilderExtensions
{
    public static IRpcBuilder AddConfigBuilder(this IRpcBuilder builder, Type? baseReturnType)
    {
        builder.Services.TryAddSingleton<IConfigValidator, ConfigValidator>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IRouteValidator, ServicePatternValidator>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceValidator, InstanceValidator>());

        builder.Services.TryAddSingleton<AdaptiveMetrics>();

        builder.Services.TryAddEnumerable([
            ServiceDescriptor.Singleton<ILoadBalance, ConsistentHashLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, LeastActiveLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, RandomLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, RoundRobinLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, ShortestResponseLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, AdaptiveLoadBalance>(),
        ]);


        builder.Services.TryAddEnumerable([
            ServiceDescriptor.Singleton<ICluster, BroadcastCluster>(),
            ServiceDescriptor.Singleton<ICluster, FailbackCluster>(),
            ServiceDescriptor.Singleton<ICluster, FailfastCluster>(),
            ServiceDescriptor.Singleton<ICluster, FailoverCluster>(),
            ServiceDescriptor.Singleton<ICluster, FailsafeCluster>(),
            ServiceDescriptor.Singleton<ICluster, ForkingCluster>(),
            ServiceDescriptor.Singleton<ICluster, MergeableCluster>(),
            //ServiceDescriptor.Singleton<ICluster, AvailableCluster>(),
        ]);

        builder.Services.TryAddEnumerable([
            ServiceDescriptor.Singleton<IMerger, ArrayMerger>(),
        ServiceDescriptor.Singleton<IMerger, BooleanArrayMerger>(),
        ServiceDescriptor.Singleton<IMerger, ByteArrayMerger>(),
        ServiceDescriptor.Singleton<IMerger, CharArrayMerger>(),
        ServiceDescriptor.Singleton<IMerger, DoubleArrayMerger>(),
        ServiceDescriptor.Singleton<IMerger, FloatArrayMerger>(),
        ServiceDescriptor.Singleton<IMerger, ShortArrayMerger>(),
        ServiceDescriptor.Singleton<IMerger, IntArrayMerger>(),
        ServiceDescriptor.Singleton<IMerger, LongArrayMerger>(),
        //ServiceDescriptor.Singleton(typeof(IMerger), typeof(ListMerger<>)),
        //ServiceDescriptor.Singleton(typeof(IMerger), typeof(DictionaryMerger<,>)),
        //ServiceDescriptor.Singleton(typeof(IMerger), typeof(SetMerger<>)),
        ]);



        builder.Services.TryAddEnumerable(
        [
            ServiceDescriptor.Singleton<ICache, LocalCache>(),
            ServiceDescriptor.Singleton<ICache, LruCache>(),
            ServiceDescriptor.Singleton<ICache, ThreadLocalCache>(),
            ServiceDescriptor.Singleton<ICache, AsyncLocalCache>(),
        ]);


        builder.Services.TryAddEnumerable([
            ServiceDescriptor.Singleton<IStateRouterFactory, ConditionStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, AppStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, ServiceStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, MockStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, TagStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, ScriptStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, FileStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, NoneStateRouterFactory>(),
        ]);


        builder.Services.TryAddSingleton<IZooyardPools>((serviceProvder) =>
        {
            var loggerfactory = serviceProvder.GetRequiredService<ILoggerFactory>();
            var lookup = serviceProvder.GetRequiredService<IRpcStateLookup>();
            
            var pools = serviceProvder.GetServices<IClientPool>();

            var clientPools = new Dictionary<string, IClientPool>();
            foreach (var pool in pools)
            {
                clientPools.Add(pool.Name!, pool);
            }

            var loadBalances = serviceProvder.GetServices<ILoadBalance>();
            var clusters = serviceProvder.GetServices<ICluster>();
            var caches = serviceProvder.GetServices<ICache>();
            var routeFactories = serviceProvder.GetServices<IStateRouterFactory>();
            var zooyardPools = new ZooyardPools(loggerfactory, clientPools, loadBalances, clusters, caches, routeFactories, lookup, baseReturnType);
            return zooyardPools;
        });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigChangeListener, ConfigChangeListener>());

        return builder;
    }

    public static IRpcBuilder AddRuntimeStateManagers(this IRpcBuilder builder)
    {
        //builder.Services.TryAddSingleton<IDestinationHealthUpdater, DestinationHealthUpdater>();
        //builder.Services.TryAddSingleton<IClusterDestinationsUpdater, ClusterDestinationsUpdater>();
        //builder.Services.TryAddEnumerable(new[] {
        //    ServiceDescriptor.Singleton<IAvailableDestinationsPolicy, HealthyAndUnknownDestinationsPolicy>(),
        //    ServiceDescriptor.Singleton<IAvailableDestinationsPolicy, HealthyOrPanicDestinationsPolicy>()
        //});
        return builder;
    }

    public static IRpcBuilder AddConfigManager(this IRpcBuilder builder)
    {
        builder.Services.TryAddSingleton<RpcConfigManager>();
        builder.Services.TryAddSingleton<IRpcStateLookup>(sp => sp.GetRequiredService<RpcConfigManager>());
        return builder;
    }

    //public static IRpcBuilder AddPassiveHealthCheck(this IRpcBuilder builder)
    //{
    //    //builder.Services.AddSingleton<IPassiveHealthCheckPolicy, TransportFailureRateHealthPolicy>();
    //    return builder;
    //}

    public static IRpcBuilder AddInstanceResolver(this IRpcBuilder builder)
    {
        builder.Services.TryAddSingleton<IInstanceResolver, NoOpInstanceResolver>();
        return builder;
    }
}
