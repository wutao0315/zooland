﻿using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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


        private readonly IDictionary<string, ChannelCredentials> _credentials;
        private readonly IDictionary<string, Type> _grpcClientTypes;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public GrpcClientPool(
            IDictionary<string, ChannelCredentials> credentials,
            IDictionary<string, Type> grpcClientTypes,
            ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _credentials = credentials;
            _grpcClientTypes = grpcClientTypes;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<GrpcClientPool>();
        }

        //private AsyncAuthInterceptor CreateAuthInterceptor()
        //{
        //    return (context, metadata) =>
        //    {
        //        var entry = new Metadata.Entry("authentication", "");
        //        if (entry != null)
        //        {
        //            metadata.Add(entry);
        //        }
        //        return Task.CompletedTask;
        //    };
        //}

        protected override IClient CreateClient(URL url)
        {
            //实例化TheTransport
            //获得transport参数,用于反射实例化
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);


            var proxyKey = url.GetParameter(PROXY_KEY);
            if (string.IsNullOrEmpty(proxyKey) || !_grpcClientTypes.ContainsKey(proxyKey))
            {
                throw new RpcException("not find the proxy grpc client");
            }

            var maxReceiveMessageLength = url.GetParameter(MAXLENGTH_KEY, DEFAULT_MAXLENGTH);
            var options = new List<ChannelOption>
            {
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, maxReceiveMessageLength)
            };

            //ChannelCredentials.Create(ChannelCredentials.Insecure, CallCredentials.FromInterceptor(CreateAuthInterceptor()));
            var credentials = ChannelCredentials.Insecure;
            

            var credentialsKey = url.GetParameter(CREDENTIALS_KEY, DEFAULT_CREDENTIALS);
            if (_credentials != null 
                && _credentials.ContainsKey(credentialsKey) 
                && credentialsKey!= DEFAULT_CREDENTIALS)
            {
                credentials = _credentials[credentialsKey];
            }

            var channel = new Channel(url.Host, url.Port, credentials, options);


            //实例化GrpcClient
            var client = Activator.CreateInstance(_grpcClientTypes[proxyKey], channel);

            return new GrpcClient(channel, client, url, credentials, timeout, _loggerFactory);
        }
    }
}
