using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Thrift.Server;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftServer : AbstractServer
    {
        private readonly ILogger _logger;
        private readonly TBaseServer _server;
        public ThriftServer(TBaseServer server , ILoggerFactory loggerFactory):base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ThriftServer>();
            _server = server;
        }
        
        
        public override async Task DoExport()
        {
            //run the server
            await Task.Run(async()=> 
            {
                await _server.ServeAsync(CancellationToken.None);
            });

            _logger.LogInformation($"Started the thrift server ...");
            Console.WriteLine($"Started the thrift server ...");
        }

        public override async Task DoDispose()
        {
            //unregiste from register center
            await Task.CompletedTask;
            _server.Stop();
            _logger.LogInformation("stoped the thrift server ...");
        }
    }
}
