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

namespace Zooyard.Core.Extensions
{
    public class ZoolandOption
    {
        public string RegisterUrl { get; set; }
        public IList<string> ClientUrls { get; set; }
        public IDictionary<string, string> ClientPools { get; set; }
        public IList<ZoolandClientOption> Clients { get; set; }
    }
    public class ZoolandClientOption
    {
        public string App { get; set; }
        public string Version { get; set; }
        public string ServiceType { get; set; }
    }

    public static class ServiceBuilderExtensions
    {
        public static void AddZoolandClient(this IServiceCollection services, IConfiguration config)
        {
            
            services.AddSingleton<IDictionary<string, IClientPool>>((serviceProvder) =>
            {
                var option = serviceProvder.GetService<IOptions<ZoolandOption>>().Value;
                var result = new Dictionary<string, IClientPool>();

                foreach (var item in option.ClientPools)
                {
                    var pool = serviceProvder.GetService(Type.GetType(item.Value)) as IClientPool;
                    result.Add(item.Key, pool);
                }
                return result;
            });

            services.AddSingleton<ConsistentHashLoadBalance>();
            services.AddSingleton<LeastActiveLoadBalance>();
            services.AddSingleton<RandomLoadBalance>();
            services.AddSingleton<RoundRobinLoadBalance>();


            services.AddSingleton<BroadcastCluster>();
            services.AddSingleton<FailbackCluster>();
            services.AddSingleton<FailfastCluster>();
            services.AddSingleton<FailoverCluster>();
            services.AddSingleton<FailsafeCluster>();
            services.AddSingleton<ForkingCluster>();
            services.AddSingleton<MergeableCluster>();


            var caches = new Dictionary<string, Type>
            {
                {"local",typeof(LocalCache) },
                {"lru",typeof(LruCache) },
                {"threadlocal",typeof(ThreadLocalCache) },
            };

            services.AddSingleton<IZooyardPools>((serviceProvder)=> 
            {
                var option = serviceProvder.GetService<IOptions<ZoolandOption>>().Value;
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
            config.Bind("zooyard", optionData);

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
        public static void AddZoolandServer(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvder) =>
            {
                var loggerFactory = serviceProvder.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Zooyard.Core.Extensions");
                var servers = serviceProvder.GetServices<IServer>();
                foreach (var server in servers)
                {
                    try
                    {
                        server.Export();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                    }
                }
                return servers;
            });
        }
    }
}
