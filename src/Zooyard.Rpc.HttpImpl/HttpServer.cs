
using Microsoft.Extensions.Logging;
using System;
using Zooyard.Rpc.Support;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpServer : AbstractServer
    {
        private readonly ILogger _logger;
        private readonly IWebHost _server;
        public HttpServer(IWebHost server, IRegistryService registryService, ILoggerFactory loggerFactory)
            : base(registryService, loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpServer>();
            _server = server;
        }


        public override async Task DoExport()
        {
            try
            {
                await _server.RunAsync();
                _logger.LogDebug("http server started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

        }

        public override async Task DoDispose()
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }
}
