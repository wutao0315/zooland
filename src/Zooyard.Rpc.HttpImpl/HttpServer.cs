using System;
using Zooyard.Rpc.Support;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpServer : AbstractServer
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpServer));
        private readonly IWebHost _server;
        public HttpServer(IWebHost server, IRegistryService registryService)
            : base(registryService)
        {
            _server = server;
        }


        public override async Task DoExport()
        {
            try
            {
                await _server.RunAsync();
                Logger().LogDebug("http server started");
            }
            catch (Exception ex)
            {
                Logger().LogError(ex, ex.Message);
            }

        }

        public override async Task DoDispose()
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }
}
