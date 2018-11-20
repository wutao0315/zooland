using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcServer : AbstractServer
    {
        private readonly IEnumerable<ServerServiceDefinition> _services;
        private readonly IEnumerable<ServerPort> _ports;

        private readonly ILogger _logger;
        private readonly Server _server;
        public GrpcServer(Server server, IEnumerable<ServerServiceDefinition> services, IEnumerable<ServerPort> ports, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GrpcServer>();
            _server = server;
            _services = services;
            _ports = ports;
        }


        public override void DoExport()
        {
            foreach (var item in _services)
            {
                _server.Services.Add(item);
            }
            foreach (var item in _ports)
            {
                _server.Ports.Add(item);
            }
            //开启服务
            _server.Start();
            _logger.LogInformation("started the grpc server ...");
        }

        public override void DoDispose()
        {
            //向注册中心发送注销请求
            if (_server != null)
            {
                _server.ShutdownAsync().GetAwaiter().GetResult();
            }
        }
    }
}
