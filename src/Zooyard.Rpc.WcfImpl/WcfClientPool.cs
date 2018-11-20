using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfClientPool : AbstractClientPool
    {
        public const string BINDING_KEY = "binding";
        public const string DEFAULT_BINDING = "http";
        public const string PATH_KEY = "path";
        public const string PROXY_KEY = "proxy";
        public const string TIMEOUT_KEY = "wcf_timeout";
        public const int DEFAULT_TIMEOUT = 5000;

        private readonly IDictionary<string, Type> _channelTypes;
        private readonly IDictionary<string, Type> _bindingTypes;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        public WcfClientPool(IDictionary<string, Type> channelTypes, IDictionary<string, Type> bindingTypes, ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            _channelTypes = channelTypes;
            _bindingTypes = bindingTypes;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WcfClientPool>();
        }
        /// <summary>
        /// channel factories
        /// </summary>
        protected Dictionary<string, ChannelFactory> factories = new Dictionary<string, ChannelFactory>();
        
        

        protected override IClient CreateClient(URL url)
        {
            //create wcf client
            
            ChannelFactory factory;

            var proxyKey = url.GetParameter(PROXY_KEY);
            if (string.IsNullOrEmpty(proxyKey) || !_channelTypes.ContainsKey(proxyKey))
            {
                throw new RpcException("not find the proxy wcf client");
            }

            Type channelType = _channelTypes[proxyKey];

            if (factories.ContainsKey(url.ToIdentityString()))
            {
                factory = factories[url.ToIdentityString()];
            }
            else
            {
                var endPointAddr = new EndpointAddress($"{url.Protocol}://{url.Host}:{url.Port}/{url.Path}");

                var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
                var bindingKey = url.GetParameter(BINDING_KEY, DEFAULT_BINDING);
                var binding = (Binding)Activator.CreateInstance(_bindingTypes[bindingKey]);
                binding.SendTimeout = TimeSpan.FromMilliseconds(timeout);
                binding.ReceiveTimeout = TimeSpan.FromMilliseconds(timeout);
                
                factory = typeof(ChannelFactory<>)
                    .MakeGenericType(channelType)
                    .GetConstructor(new Type[] { typeof(Binding), typeof(EndpointAddress) })
                    .Invoke(new object[] { binding, endPointAddr }) as ChannelFactory;

                factories.Add(url.ToIdentityString(), factory);
            }

            var channel = factory.GetType().GetMethod("CreateChannel", new Type[] { }).Invoke(factory, null) as ICommunicationObject;

            return new WcfClient(channel, url, _loggerFactory);
        }
    }
}
