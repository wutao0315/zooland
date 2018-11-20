
using Microsoft.Extensions.Logging;
using System;
using Zooyard.Rpc.Support;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpServer : AbstractServer
    {
        private readonly ILogger _logger;
        private readonly IWebHost _server;
        public HttpServer(IWebHost server, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpServer>();
            _server = server;
        }


        public override void DoExport()
        {
            try
            {
                _server.Run();
                _logger.LogDebug("http server started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

        }

        public override void DoDispose()
        {
            _server.Dispose();
        }
    }
}
