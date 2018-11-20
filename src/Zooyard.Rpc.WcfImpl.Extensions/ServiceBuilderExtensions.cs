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

namespace Zooyard.Rpc.WcfImpl.Extensions
{
    public class WcfOption
    {
        public IDictionary<string,string> Channels { get; set; }
        public IDictionary<string, string> Bindings { get; set; }
    }

    public class WcfServerOption
    {
        public string ProtocolFactoryType { get; set; }
        public WcfTransportOption Transport { get; set; }
    }
    public class WcfTransportOption
    {
        public string TransportType { get; set; }
        public int Port { get; set; }
        public int ClientTimeOut { get; set; }
        public bool UserBufferedSockets { get; set; }
    }

    public static class ServiceBuilderExtensions
    {
        public static void AddWcfClient(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvder) => 
            {
                var option = serviceProvder.GetService<IOptions<WcfOption>>().Value;
                var loggerFactory = serviceProvder.GetService<ILoggerFactory>();
                var channelTypes = new Dictionary<string, Type>();
                foreach (var item in option.Channels)
                {
                    channelTypes.Add(item.Key, Type.GetType(item.Value));
                }

                var bindingTypes = new Dictionary<string, Type>();
                foreach (var item in option.Bindings)
                {
                    bindingTypes.Add(item.Key, Type.GetType(item.Value));
                }

                var pool = new WcfClientPool(
                    channelTypes: channelTypes,
                    bindingTypes: bindingTypes,
                    loggerFactory: loggerFactory
                );

                return pool;
            });

        }

        public static void AddWcfServer<ifacade, facade>(this IServiceCollection services)
            where ifacade : class
            where facade : class,ifacade
        {
            services.AddTransient<ifacade, facade>();
          

        }
    }
}
