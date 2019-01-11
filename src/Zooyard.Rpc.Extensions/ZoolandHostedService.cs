using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.Extensions
{
    public class ZoolandHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IServer _server;

        public ZoolandHostedService(ILogger<ZoolandHostedService> logger, IServer server)
        {
            _logger = logger;
            _server = server;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Zooland started...");
            Console.WriteLine("Zooland started...");
            _server.Export();
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Zooland stopped...");
            Console.WriteLine("Zooland stopped...");
            _server.Dispose();
            await Task.CompletedTask;
        }
    }
}
