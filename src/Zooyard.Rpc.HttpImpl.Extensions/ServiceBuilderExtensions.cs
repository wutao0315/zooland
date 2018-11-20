using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Zooyard.Core;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;

namespace Zooyard.Rpc.HttpImpl.Extensions
{
    public class ThriftOption
    {
        public IDictionary<string,string> Clients { get; set; }
    }

    public class ThriftServerOption
    {
        public string ProtocolFactoryType { get; set; }
        public ThriftTransportOption Transport { get; set; }
    }
    public class ThriftTransportOption
    {
        public string TransportType { get; set; }
        public int Port { get; set; }
        public int ClientTimeOut { get; set; }
        public bool UserBufferedSockets { get; set; }
    }

    public static class ServiceBuilderExtensions
    {
        public static void AddHttpClient(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvder) => 
            {
                var option = serviceProvder.GetService<IOptions<ThriftOption>>().Value;
                var loggerFactory = serviceProvder.GetService<ILoggerFactory>();
                var thriftClientTypes = new Dictionary<string, Type>();
                foreach (var item in option.Clients)
                {
                    thriftClientTypes.Add(item.Key, Type.GetType(item.Value));
                }

                var pool = new HttpClientPool(
                    loggerFactory: loggerFactory
                );

                return pool;
            });

        }

        public static void AddHttpServer<ifacade, facade>(this IServiceCollection services)
            where ifacade : class
            where facade : class,ifacade
        {
            services.AddTransient<ifacade, facade>();
          

        }
    }
}
