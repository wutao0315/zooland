﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;
using Zooyard.DataAnnotations;
using Zooyard.Logging;
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

namespace Zooyard;

public static class ServiceBuilderExtensions
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ServiceBuilderExtensions));
    public static void AddZoolandClient(this IServiceCollection services, params Type[] types)
    {

        services.AddSingleton<ILoadBalance, ConsistentHashLoadBalance>();
        services.AddSingleton<ILoadBalance, LeastActiveLoadBalance>();
        services.AddSingleton<ILoadBalance, RandomLoadBalance>();
        services.AddSingleton<ILoadBalance, RoundRobinLoadBalance>();


        services.AddSingleton<ICluster, BroadcastCluster>();
        services.AddSingleton<ICluster, FailbackCluster>();
        services.AddSingleton<ICluster, FailfastCluster>();
        services.AddSingleton<ICluster, FailoverCluster>();
        services.AddSingleton<ICluster, FailsafeCluster>();
        services.AddSingleton<ICluster, ForkingCluster>();


        services.AddSingleton<IMerger, ArrayMerger>();
        services.AddSingleton<IMerger, BooleanArrayMerger>();
        services.AddSingleton<IMerger, ByteArrayMerger>();
        services.AddSingleton<IMerger, CharArrayMerger>();
        services.AddSingleton<IMerger, DoubleArrayMerger>();
        services.AddSingleton<IMerger, FloatArrayMerger>();
        services.AddSingleton<IMerger, ShortArrayMerger>();
        services.AddSingleton<IMerger, IntArrayMerger>();
        services.AddSingleton<IMerger, LongArrayMerger>();

        //services.AddSingleton(typeof(IMerger), typeof(ListMerger<>));
        //services.AddSingleton(typeof(IMerger), typeof(DictionaryMerger<,>));
        //services.AddSingleton(typeof(IMerger), typeof(SetMerger<>));

        services.AddSingleton<ICluster, MergeableCluster>();

        services.AddSingleton<ICache, LocalCache>();
        services.AddSingleton<ICache, LruCache>();
        services.AddSingleton<ICache, ThreadLocalCache>();
        services.AddSingleton<ICache, AsyncLocalCache>();


        services.AddSingleton<IStateRouterFactory, ConditionStateRouterFactory>();
        services.AddSingleton<IStateRouterFactory, AppStateRouterFactory>();
        services.AddSingleton<IStateRouterFactory, ServiceStateRouterFactory>();
        services.AddSingleton<IStateRouterFactory, MockStateRouterFactory>();
        services.AddSingleton<IStateRouterFactory, TagStateRouterFactory>();
        services.AddSingleton<IStateRouterFactory, ScriptStateRouterFactory>();
        services.AddSingleton<IStateRouterFactory, FileStateRouterFactory>();
        services.AddSingleton<IStateRouterFactory, NoneStateRouterFactory>();


        services.AddSingleton<IZooyardPools>((serviceProvder) =>
        {
            var option = serviceProvder.GetRequiredService<IOptionsMonitor<ZooyardOption>>();

            var clientPools = new Dictionary<string, IClientPool>();
            foreach (var serviceType in types)
            {
                var zooyard = serviceType.GetCustomAttribute<ZooyardAttribute>();
                if (zooyard == null)
                {
                    throw new Exception($"{nameof(ZooyardAttribute)} is not exists");
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

            var zooyardPools = new ZooyardPools(clientPools, loadBalances, clusters, caches, routeFactories, option);
            return zooyardPools;
        });


        foreach (var serviceType in types)
        {
            var genericType = typeof(ZooyardFactory<>);
            var factoryType = genericType.MakeGenericType(new[] { serviceType });
            services.AddSingleton(factoryType, (serviceProvder) =>
            {
                var pools = serviceProvder.GetRequiredService<IZooyardPools>();
                var constructor = factoryType.GetConstructor(new[] { typeof(IZooyardPools), typeof(string), typeof(string), typeof(string) });
                if (constructor == null)
                {
                    throw new Exception($"{nameof(constructor)} is not exists");
                }
                var zooyard = serviceType.GetCustomAttribute<ZooyardAttribute>();
                if (zooyard == null)
                {
                    throw new Exception($"{nameof(ZooyardAttribute)} is not exists");
                }
                var zooyardFactory = constructor.Invoke(new object[] { pools, zooyard.ServiceName, zooyard.Version, zooyard.Url });
                return zooyardFactory;
            });


            services.AddTransient(serviceType, (serviceProvder) =>
            {
                var factory = serviceProvder.GetRequiredService(factoryType);
                var createYardMethod = factoryType.GetMethod("CreateYard");
                if (createYardMethod == null)
                {
                    throw new Exception($"{nameof(createYardMethod)} is not exists");
                }
                var result = createYardMethod.Invoke(factory, null);
                if (result == null)
                {
                    throw new Exception($"{nameof(createYardMethod)} is not null");
                }
                return result;
            });
        }
    }
}