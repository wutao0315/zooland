using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Zooyard.Core;
using Zooyard.Rpc;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;
using Zooyard.Rpc.Merger;

namespace Zooyard.Core.Extensions
{
    public class ZoolandOption
    {
        public string RegisterUrl { get; set; }
        public IList<string> ClientUrls { get; set; }
        public IDictionary<string, string> ClientPools { get; set; }
        public IList<ZoolandClientOption> Clients { get; set; }
        public IDictionary<string, string> Mergers { get; set; }
    }
    public class ZoolandClientOption
    {
        public string App { get; set; }
        public string Version { get; set; }
        public string ServiceType { get; set; }
    }

    public static class ServiceBuilderExtensions
    {
        public static void AddZoolandClient(this IServiceCollection services, IConfiguration config, string zooyard= "zooyard")
        {
            
            services.AddSingleton<IDictionary<string, IClientPool>>((serviceProvder) =>
            {
                var option = serviceProvder.GetService<IOptionsMonitor<ZoolandOption>>().CurrentValue;
                var result = new Dictionary<string, IClientPool>();

                foreach (var item in option.ClientPools)
                {
                    var pool = serviceProvder.GetService(Type.GetType(item.Value)) as IClientPool;
                    result.Add(item.Key, pool);
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
                    {typeof(Array),serviceProvder.GetService<ArrayMerger>()},
                    {typeof(bool),serviceProvder.GetService<BooleanArrayMerger>()},
                    {typeof(byte),serviceProvder.GetService<ByteArrayMerger>()},
                    {typeof(char),serviceProvder.GetService<CharArrayMerger>()},
                    {typeof(double),serviceProvder.GetService<DoubleArrayMerger>()},
                    {typeof(float),serviceProvder.GetService<FloatArrayMerger>()},
                    {typeof(short), serviceProvder.GetService<ShortArrayMerger>()},
                    {typeof(int),serviceProvder.GetService<IntArrayMerger>()},
                    {typeof(long),serviceProvder.GetService<LongArrayMerger>() },
                    {typeof(IEnumerable<>),serviceProvder.GetService(typeof(ListMerger<>)) as IMerger},
                    {typeof(IDictionary<,>),serviceProvder.GetService(typeof(DictionaryMerger<,>)) as IMerger},
                    {typeof(ISet<>), serviceProvder.GetService(typeof(SetMerger<>)) as IMerger}
                };
                return result;
            });
            services.AddSingleton<IDictionary<string, IMerger>>((serviceProvder) =>
            {
                var option = serviceProvder.GetService<IOptionsMonitor<ZoolandOption>>().CurrentValue;
                var result = new Dictionary<string, IMerger>();

                foreach (var item in option.Mergers)
                {
                    var merger = serviceProvder.GetService(Type.GetType(item.Value)) as IMerger;
                    result.Add(item.Key, merger);
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
                var option = serviceProvder.GetService<IOptionsMonitor<ZoolandOption>>().CurrentValue;
                var clientPools = serviceProvder.GetService<IDictionary<string,IClientPool>>();
                var loggerFactory = serviceProvder.GetService<ILoggerFactory>();

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

                var zooyardPools = new ZooyardPools(loggerFactory, clientPools, loadBalances, clusters, caches, option.RegisterUrl, option.ClientUrls);
                return zooyardPools;
            });

            var optionData = new ZoolandOption();
            config.Bind(zooyard, optionData);

            foreach (var item in optionData.Clients)
            {
                var serviceType = Type.GetType(item.ServiceType);
                var genericType = typeof(ZooyardFactory<>);
                var factoryType = genericType.MakeGenericType(new []{ serviceType });
                services.AddSingleton(factoryType, (serviceProvder) =>
                {
                    var pools = serviceProvder.GetService<IZooyardPools>();
                    var zooyardFactory = factoryType.GetConstructor(new[] { typeof(IZooyardPools),typeof(string),typeof(string) })
                    .Invoke(new object[] { pools,item.App,item.Version });
                    return zooyardFactory;
                });

                
                services.AddTransient(serviceType, (serviceProvder) =>
                {
                    var factory = serviceProvder.GetService(factoryType);
                    var result = factory.GetType().GetMethod("CreateYard").Invoke(factory,null);
                    return result;
                });
            }
            

        }
        //public static void AddZoolandServer(this IServiceCollection services)
        //{
          
        //}
    }
}
