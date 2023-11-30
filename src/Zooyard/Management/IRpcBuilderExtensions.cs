using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zooyard.Configuration;
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

namespace Zooyard.Management;

internal static class IRpcBuilderExtensions
{
    public static IRpcBuilder AddConfigBuilder(this IRpcBuilder builder)
    {
        builder.Services.TryAddSingleton<IConfigValidator, ConfigValidator>();

        builder.Services.TryAddSingleton<AdaptiveMetrics>();

        builder.Services.TryAddEnumerable(new[] { 
            ServiceDescriptor.Singleton<ILoadBalance, ConsistentHashLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, LeastActiveLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, RandomLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, RoundRobinLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, ShortestResponseLoadBalance>(),
            ServiceDescriptor.Singleton<ILoadBalance, AdaptiveLoadBalance>(),
        });


        builder.Services.TryAddEnumerable(new[] { 
            ServiceDescriptor.Singleton<ICluster, BroadcastCluster>(),
            ServiceDescriptor.Singleton<ICluster, FailbackCluster>(),
            ServiceDescriptor.Singleton<ICluster, FailfastCluster>(),
            ServiceDescriptor.Singleton<ICluster, FailoverCluster>(),
            ServiceDescriptor.Singleton<ICluster, FailsafeCluster>(),
            ServiceDescriptor.Singleton<ICluster, ForkingCluster>(),
            ServiceDescriptor.Singleton<ICluster, MergeableCluster>(),
            //ServiceDescriptor.Singleton<ICluster, AvailableCluster>(),
        });

        builder.Services.TryAddEnumerable(new[] {
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
        });



        builder.Services.TryAddEnumerable(new[] 
        { 
            ServiceDescriptor.Singleton<ICache, LocalCache>(),
            ServiceDescriptor.Singleton<ICache, LruCache>(),
            ServiceDescriptor.Singleton<ICache, ThreadLocalCache>(),
            ServiceDescriptor.Singleton<ICache, AsyncLocalCache>(),
        });


        builder.Services.TryAddEnumerable(new[]{
            ServiceDescriptor.Singleton<IStateRouterFactory, ConditionStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, AppStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, ServiceStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, MockStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, TagStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, ScriptStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, FileStateRouterFactory>(),
            ServiceDescriptor.Singleton<IStateRouterFactory, NoneStateRouterFactory>(),
        });



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
