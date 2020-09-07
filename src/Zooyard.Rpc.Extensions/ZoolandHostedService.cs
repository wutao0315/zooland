using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;

namespace Zooyard.Rpc.Extensions
{
    public class ZoolandHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IServer> _servers;

        public ZoolandHostedService(IEnumerable<IServer> servers, ILogger<ZoolandHostedService> logger)
        {
            _servers = servers;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Zooland started...");
            Console.WriteLine("Zooland started...");
            try
            {
                foreach (var server in _servers)
                {
                    await server.Export().ConfigureAwait(false);
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
