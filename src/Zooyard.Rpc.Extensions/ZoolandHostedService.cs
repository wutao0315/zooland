﻿using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.Extensions
{
    public class ZoolandHostedService : IHostedService
    {
        private readonly IEnumerable<IServer> _servers;

        public ZoolandHostedService(IEnumerable<IServer> servers)
        {
            _servers = servers;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
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
                Console.WriteLine($"{ex.Message}:{ex.StackTrace}");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
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
                Console.WriteLine($"{ex.Message}:{ex.StackTrace}");
            }
        }
    }
}
