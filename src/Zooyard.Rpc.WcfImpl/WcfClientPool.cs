using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Rpc.Support;
using Zooyard.Rpc.WcfImpl;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfClientPool : AbstractClientPool
    {
        //public const string BINDING_KEY = "binding";
        public const string DEFAULT_BINDING = "BasicHttpBinding";
        public const string PATH_KEY = "path";

        public const string PROXY_KEY = "proxy";

        public const string TIMEOUT_KEY = "wcf_timeout";
        public const int DEFAULT_TIMEOUT = 5000;
        /// <summary>
        /// 通道工厂
        /// </summary>
        protected Dictionary<string, ChannelFactory> factories = new Dictionary<string, ChannelFactory>();
        
        public IDictionary<string, Type> TheChannelTypes { get; set; }
        public IDictionary<string, Type> TheBindingTypes { get; set; }

        protected override IClient CreateClient(URL url)
        {
            //实例化TheTransport
            //获得transport参数,用于反射实例化
            
            ChannelFactory factory;

            var proxyKey = url.GetParameter(PROXY_KEY);
            if (string.IsNullOrEmpty(proxyKey) || !TheChannelTypes.ContainsKey(proxyKey))
            {
                throw new RpcException("not find the proxy wcf client");
            }

            Type channelType = TheChannelTypes[proxyKey];

            if (factories.ContainsKey(url.ToIdentityString()))
            {
                factory = factories[url.ToIdentityString()];
            }
            else
            {
                var endPointAddr = new EndpointAddress($"{url.Protocol}://{url.Host}:{url.Port}/{url.Path}");

                var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
                //获取协议
                Binding binding = new BasicHttpBinding();
                if (TheBindingTypes.ContainsKey(url.Protocol))
                {
                    binding = (Binding)Activator.CreateInstance(TheBindingTypes[url.Protocol]);
                }
                binding.SendTimeout = TimeSpan.FromMilliseconds(timeout);
                binding.ReceiveTimeout = TimeSpan.FromMilliseconds(timeout);
                
                factory = typeof(ChannelFactory<>)
                    .MakeGenericType(channelType)
                    .GetConstructor(new Type[] { typeof(Binding), typeof(EndpointAddress) })
                    .Invoke(new object[] { binding, endPointAddr }) as ChannelFactory;

                factories.Add(url.ToIdentityString(), factory);
            }

            var channel = factory.GetType().GetMethod("CreateChannel", new Type[] { }).Invoke(factory, null) as ICommunicationObject;

            return new WcfClient(channel, url);
        }
    }
}
