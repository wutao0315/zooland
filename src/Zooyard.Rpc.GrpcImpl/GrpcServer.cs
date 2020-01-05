using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcServer : AbstractServer
    {
        private readonly IEnumerable<ServerServiceDefinition> _services;
        private readonly IEnumerable<ServerPort> _ports;
        private readonly IEnumerable<ServerInterceptor> _interceptors;

        private readonly ILogger _logger;
        private readonly Server _server;
        public GrpcServer(Server server, 
            IEnumerable<ServerServiceDefinition> services,
            IEnumerable<ServerPort> ports,
            IEnumerable<ServerInterceptor> interceptors,
            ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GrpcServer>();
            _server = server;
            _services = services;
            _ports = ports;
            _interceptors = interceptors;
        }


        public override void DoExport()
        {
            foreach (var item in _services)
            {
                if (_interceptors?.Count() > 0) 
                {
                    item.Intercept(_interceptors.ToArray());
                }
                _server.Services.Add(item);
            }
            foreach (var item in _ports)
            {
                _server.Ports.Add(item);
            }
            //开启服务
            _server.Start();
            _logger.LogInformation($"Started the grpc server ...");
            Console.WriteLine($"Started the grpc server ...");
        }

        public override void DoDispose()
        {
            //向注册中心发送注销请求
            if (_server != null)
            {
                _server.ShutdownAsync().ConfigureAwait(false);
            }
        }
    }
}
