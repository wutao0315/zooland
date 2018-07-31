using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Transport;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftClientPool : AbstractClientPool
    {
        public const string TRANSPORT_KEY = "transport";
        public const string DEFAULT_TRANSPORT = "TSocket";

        //public const string PROTOCOL_KEY = "protocol";
        public const string DEFAULT_PROTOCOL = "TBinaryProtocol";

        public const string PROXY_KEY = "proxy";

        public const string TIMEOUT_KEY = "tf_timeout";
        public const int DEFAULT_TIMEOUT = 5000;

        public IDictionary<string,Type>  TheTransportTypes { get; set; }
        public IDictionary<string, Type> TheProtocolTypes { get; set; }
        public IDictionary<string, Type> TheThriftClientTypes { get; set; }

        protected override IClient CreateClient(URL url)
        {
            //实例化TheTransport
            //获得transport参数,用于反射实例化
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
            TTransport transport = new TSocket(url.Host,url.Port, timeout);
            var transportKey = url.GetParameter(TRANSPORT_KEY, DEFAULT_TRANSPORT);
            if (TheTransportTypes!=null 
                && TheTransportTypes.ContainsKey(transportKey)
                && transportKey!= DEFAULT_TRANSPORT)
            {
                transport = (TTransport)Activator.CreateInstance(TheTransportTypes[transportKey], url.Host, url.Port, timeout);
            }

            //获取协议
            TProtocol protocol = new TBinaryProtocol(transport);
            //var protocolKey = url.GetParameter(PROTOCOL_KEY, DEFAULT_PROTOCOL);
            if (TheProtocolTypes!=null 
                && TheProtocolTypes.ContainsKey(url.Protocol) 
                && url.Protocol != DEFAULT_PROTOCOL)
            {
                protocol = (TProtocol)Activator.CreateInstance(TheProtocolTypes[url.Protocol], transport);
            }

            var proxyKey = url.GetParameter(PROXY_KEY);
            if (string.IsNullOrEmpty(proxyKey) || !TheThriftClientTypes.ContainsKey(proxyKey))
            {
                throw new RpcException($"not find the proxy thrift client{url.ToFullString()}");
            }
            //实例化TheThriftClient
            var client = (IDisposable)Activator.CreateInstance(TheThriftClientTypes[proxyKey], protocol);

            return new ThriftClient(transport, client, url);
        }
    }
}
