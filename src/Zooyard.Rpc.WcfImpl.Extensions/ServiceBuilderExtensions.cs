using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
#if NET461
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Linq;
using System.ServiceModel.Channels;
using Zooyard.Core;
#endif 

namespace Zooyard.Rpc.WcfImpl.Extensions
{
    public class WcfOption
    {
        public IDictionary<string,string> Channels { get; set; }
        public IDictionary<string, string> Bindings { get; set; }
    }
#if NET461
    public class WcfServerOption
    {
        public string InstanceType { get; set; }
        public string ContractType { get; set; }
        public string BindingType { get; set; }
        public IEnumerable<string> BaseAddresses { get; set; }
    }
#endif

    public static class ServiceBuilderExtensions
    {
        public static void AddWcfClient(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvider) => 
            {
                var option = serviceProvider.GetService<IOptions<WcfOption>>().Value;
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
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
#if NET461
        public static void AddWcfServer(this IServiceCollection services, IEnumerable<IServiceBehavior> behaviors= null)
        {

            services.AddSingleton<WSHttpBinding>();
            services.AddSingleton<NetTcpBinding>();
            services.AddSingleton<BasicHttpBinding>();
            services.AddSingleton<NetHttpBinding>();

            behaviors = behaviors ?? new List<IServiceBehavior> { new ServiceMetadataBehavior() };
            

            services.AddSingleton<IList<Uri>>((serviceProvider) => 
            {
                var option = serviceProvider.GetService<IOptions<WcfServerOption>>().Value;
                var result = new List<Uri>();
                foreach (var item in option.BaseAddresses)
                {
                    result.Add(new Uri(item));
                }
                return result;
            });

            services.AddSingleton((serviceProvider)=> 
            {
                var option = serviceProvider.GetService<IOptions<WcfServerOption>>().Value;
                var instance = serviceProvider.GetService(Type.GetType(option.InstanceType));
                var binding = serviceProvider.GetService(Type.GetType(option.BindingType)) as Binding;
                var baseAddresses = serviceProvider.GetService<IList<Uri>>();
                return new WcfService(instance,Type.GetType(option.ContractType), baseAddresses, binding, behaviors.ToList());
            });
            services.AddSingleton<IServer, WcfServer>();
        }
#endif
    }
}
