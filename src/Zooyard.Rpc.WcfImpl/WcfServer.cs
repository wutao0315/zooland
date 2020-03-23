using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfServer : AbstractServer
    {
        private readonly ILogger _logger;
        private readonly WcfService _server;
        public WcfServer(WcfService server, IRegistryService registryService, ILoggerFactory loggerFactory) 
            : base(registryService, loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WcfServer>();
            _server = server;
        }

        public IList<WcfService> Services { get; set; } = new List<WcfService>();
        public override async Task DoExport()
        {
            //open service
            _server.Open();

            await Task.CompletedTask;
            // Step 3 of the hosting procedure: Add a service endpoint.
            _logger.LogInformation($"Started the wcf server ...");
            Console.WriteLine($"Started the wcf server ...");
           
        }

        public override async Task DoDispose()
        {
            _server.Dispose();
            await Task.CompletedTask;
        }
    }
}
