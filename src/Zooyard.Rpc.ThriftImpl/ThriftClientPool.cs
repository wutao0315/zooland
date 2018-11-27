using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocols;
using Thrift.Transports;
using Thrift.Transports.Client;
using Zooyard.Core;
using Zooyard.Rpc.Support;
using Microsoft.Extensions.Logging;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftClientPool : AbstractClientPool
    {
        public const string TRANSPORT_KEY = "transport";
        public const string DEFAULT_TRANSPORT = "TSocket";
        public const string DEFAULT_PROTOCOL = "TBinaryProtocol";
        public const string PROXY_KEY = "proxy";
        public const string TIMEOUT_KEY = "tf_timeout";
        public const int DEFAULT_TIMEOUT = 5000;

        private readonly IDictionary<string, Type> _transportTypes;
        private readonly IDictionary<string, Type> _protocolTypes;
        private readonly IDictionary<string, Type> _clientTypes;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        public ThriftClientPool(ILoggerFactory loggerFactory):base(loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ThriftClientPool>();
        }
        public ThriftClientPool(IDictionary<string, Type> transportTypes,
            IDictionary<string, Type> protocolTypes,
            IDictionary<string, Type> clientTypes,
            ILoggerFactory loggerFactory) :this(loggerFactory)
        {
            _transportTypes = transportTypes;
            _protocolTypes = protocolTypes;
            _clientTypes = clientTypes;
        }

        protected override IClient CreateClient(URL url)
        {
            //实例化TheTransport
            //获得transport参数,用于反射实例化
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
            
            TClientTransport transport = new TSocketClientTransport(IPAddress.Parse(url.Host),url.Port, timeout);
            var transportKey = url.GetParameter(TRANSPORT_KEY, DEFAULT_TRANSPORT);
            if (_transportTypes != null 
                && _transportTypes.ContainsKey(transportKey)
                && transportKey!= DEFAULT_TRANSPORT)
            {
                transport = (TClientTransport)Activator.CreateInstance(_transportTypes[transportKey], IPAddress.Parse(url.Host), url.Port, timeout);
            }

            //获取协议
            TProtocol protocol = new TBinaryProtocol(transport);

            if (_protocolTypes != null 
                && _protocolTypes.ContainsKey(url.Protocol) 
                && url.Protocol != DEFAULT_PROTOCOL)
            {
                protocol = (TProtocol)Activator.CreateInstance(_protocolTypes[url.Protocol], transport);
            }

            var proxyKey = url.GetParameter(PROXY_KEY);
            if (string.IsNullOrEmpty(proxyKey) || !_clientTypes.ContainsKey(proxyKey))
            {
                throw new RpcException($"not find the proxy thrift client {url.ToFullString()}");
            }
            //instance ThriftClient
            var client = (IDisposable)Activator.CreateInstance(_clientTypes[proxyKey], protocol);

            return new ThriftClient(transport, client, url, _loggerFactory);
        }
    }
}
