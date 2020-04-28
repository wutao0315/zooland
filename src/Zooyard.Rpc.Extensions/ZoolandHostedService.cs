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
        private readonly IEnumerable<IServer> _servers;

        public ZoolandHostedService(ILogger<ZoolandHostedService> logger, IEnumerable<IServer> servers)
        {
            _logger = logger;
            _servers = servers;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Zooland started...");
            Console.WriteLine("Zooland started...");
            try
            {
                foreach (var server in _servers)
                {
                    server.Export();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Zooland stopped...");
            Console.WriteLine("Zooland stopped...");
            try
            {
                foreach (var server in _servers)
                {
                    await server.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
           
        }
    }
}
