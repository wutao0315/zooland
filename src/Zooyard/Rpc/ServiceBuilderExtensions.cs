using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zooyard;
using Zooyard.Logging;
using Zooyard.Rpc;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;
using Zooyard.Rpc.Merger;

namespace Microsoft.Extensions.Configuration;

public static class ServiceBuilderExtensions
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ServiceBuilderExtensions));
    public static void AddZoolandClient(this IServiceCollection services, IConfiguration config, string zooyard= "zooyard")
    {
        services.AddSingleton<IDictionary<string, IClientPool>>((serviceProvder) =>
        {
            var option = serviceProvder.GetRequiredService<IOptionsMonitor<ZooyardOption>>().CurrentValue;
            var result = new Dictionary<string, IClientPool>();

            foreach (var item in option.Clients)
            {
                var pool = (IClientPool)serviceProvder.GetRequiredService(Type.GetType(item.Value.PoolType)!);
                result.Add(item.Value.Service.FullName!, pool);
            }
            return result;
        });

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


        services.AddSingleton<ArrayMerger>();
        services.AddSingleton<BooleanArrayMerger>();
        services.AddSingleton<ByteArrayMerger>();
        services.AddSingleton<CharArrayMerger>();
        services.AddSingleton<DoubleArrayMerger>();
        services.AddSingleton<FloatArrayMerger>();
        services.AddSingleton<ShortArrayMerger>();
        services.AddSingleton<IntArrayMerger>();
        services.AddSingleton<LongArrayMerger>();
        services.AddSingleton(typeof(ListMerger<>));
        services.AddSingleton(typeof(DictionaryMerger<,>));
        services.AddSingleton(typeof(SetMerger<>));

        services.AddSingleton<IDictionary<Type, IMerger>>((serviceProvder) => 
        {
            var result = new Dictionary<Type, IMerger>
            {
                {typeof(Array),serviceProvder.GetRequiredService<ArrayMerger>()},
                {typeof(bool),serviceProvder.GetRequiredService<BooleanArrayMerger>()},
                {typeof(byte),serviceProvder.GetRequiredService<ByteArrayMerger>()},
                {typeof(char),serviceProvder.GetRequiredService<CharArrayMerger>()},
                {typeof(double),serviceProvder.GetRequiredService<DoubleArrayMerger>()},
                {typeof(float),serviceProvder.GetRequiredService<FloatArrayMerger>()},
                {typeof(short), serviceProvder.GetRequiredService<ShortArrayMerger>()},
                {typeof(int),serviceProvder.GetRequiredService<IntArrayMerger>()},
                {typeof(long),serviceProvder.GetRequiredService<LongArrayMerger>() },
                {typeof(IEnumerable<>), (IMerger)serviceProvder.GetRequiredService(typeof(ListMerger<>))},
                {typeof(IDictionary<,>), (IMerger)serviceProvder.GetRequiredService(typeof(DictionaryMerger<,>))},
                {typeof(ISet<>), (IMerger)serviceProvder.GetRequiredService(typeof(SetMerger<>))}
            };
            return result;
        });
        services.AddSingleton<IDictionary<string, IMerger>>((serviceProvder) =>
        {
            var option = serviceProvder.GetRequiredService<IOptionsMonitor<ZooyardOption>>().CurrentValue;
            var result = new Dictionary<string, IMerger>();

            foreach (var item in option.Mergers)
            {
                var mergerType = Type.GetType(item.Value);
                if (mergerType != null && serviceProvder.GetRequiredService(mergerType) is IMerger merger) 
                {
                    result.Add(item.Key, merger);
                }
            }
            return result;
        });

        services.AddSingleton<MergeableCluster>();


        var caches = new Dictionary<string, Type>
        {
            {"local",typeof(LocalCache) },
            {"lru",typeof(LruCache) },
            {"threadlocal",typeof(ThreadLocalCache) },
        };

        services.AddSingleton<IZooyardPools>((serviceProvder)=> 
        {
            var option = serviceProvder.GetRequiredService<IOptionsMonitor<ZooyardOption>>();
            var clientPools = serviceProvder.GetRequiredService<IDictionary<string,IClientPool>>();

            var loadbalanceList = serviceProvder.GetServices<ILoadBalance>();
            var loadBalances = new Dictionary<string, ILoadBalance>();
            foreach (var item in loadbalanceList)
            {
                loadBalances.Add(item.Name,item);
            };

            var clusterList = serviceProvder.GetServices<ICluster>();
            var clusters = new Dictionary<string, ICluster>();
            foreach (var item in clusterList)
            {
                clusters.Add(item.Name,item);
            }

            var zooyardPools = new ZooyardPools(clientPools, loadBalances, clusters, caches, option);
            return zooyardPools;
        });

        var optionData = new ZooyardOption();
        config.Bind(zooyard, optionData);

        foreach (var item in optionData.Clients)
        {
            var serviceType = Type.GetType(item.Value.ServiceType);
            if (serviceType == null) 
            {
                var msg = $"waring {zooyard} not find type {item.Value.ServiceType}";
                Logger().LogWarning(msg);
                Console.WriteLine(msg);
                continue;
            }

            var genericType = typeof(ZooyardFactory<>);
            var factoryType = genericType.MakeGenericType(new []{ serviceType });
            services.AddSingleton(factoryType, (serviceProvder) =>
            {
                var pools = serviceProvder.GetRequiredService<IZooyardPools>();
                var constructor = factoryType.GetConstructor(new[] { typeof(IZooyardPools), typeof(string), typeof(string) });
                if (constructor == null) 
                {
                    throw new Exception($"{nameof(constructor)} is not exists");
                }
                var zooyardFactory = constructor.Invoke(new object[] { pools, item.Key, item.Value.Version });
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
