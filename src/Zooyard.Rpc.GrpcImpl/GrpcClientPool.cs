using Grpc.Core;
using System;
using System.Collections.Generic;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcClientPool : AbstractClientPool
    {
        public const string PROXY_KEY = "proxy";

        public const string TIMEOUT_KEY = "grpc_timeout";
        public const int DEFAULT_TIMEOUT = 5000;

        public const string MAXLENGTH_KEY = "grpc_maxlength";
        public const int DEFAULT_MAXLENGTH = int.MaxValue;

        public const string CREDENTIALS_KEY = "protocol";
        public const string DEFAULT_CREDENTIALS = "Insecure";

        public IDictionary<string, ChannelCredentials> TheCredentials { get; set; }

        public IDictionary<string, Type> TheGrpcClientTypes { get; set; }

        protected override IClient CreateClient(URL url)
        {
            //实例化TheTransport
            //获得transport参数,用于反射实例化
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);


            var proxyKey = url.GetParameter(PROXY_KEY);
            if (string.IsNullOrEmpty(proxyKey) || !TheGrpcClientTypes.ContainsKey(proxyKey))
            {
                throw new RpcException("not find the proxy thrift client");
            }

            var maxReceiveMessageLength = url.GetParameter(MAXLENGTH_KEY, DEFAULT_MAXLENGTH);
            var options = new List<ChannelOption>
            {
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength,maxReceiveMessageLength)
            };

            var credentials = ChannelCredentials.Insecure;

            var credentialsKey = url.GetParameter(CREDENTIALS_KEY, DEFAULT_CREDENTIALS);
            if (TheCredentials!=null 
                && TheCredentials.ContainsKey(credentialsKey) 
                && credentialsKey!= DEFAULT_CREDENTIALS)
            {
                credentials = TheCredentials[credentialsKey];
            }

            Channel channel = new Channel(url.Host, url.Port, credentials, options);


            //实例化TheThriftClient
            var client = Activator.CreateInstance(TheGrpcClientTypes[proxyKey], channel);

            return new GrpcClient(channel, client, url, credentials, timeout);
        }
    }
}
