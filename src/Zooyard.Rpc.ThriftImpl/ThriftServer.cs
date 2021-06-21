using System;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Server;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftServer : AbstractServer
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ThriftServer));
        private readonly TBaseServer _server;
        public ThriftServer(TBaseServer server,
            IRegistryService registryService)
            :base(registryService)
        {
            _server = server;
        }
        
        
        public override async Task DoExport()
        {
            //run the server
            await Task.Run(async()=> 
            {
                await _server.ServeAsync(CancellationToken.None);
            });

            Logger().LogInformation($"Started the thrift server ...");
            Console.WriteLine($"Started the thrift server ...");
        }

        public override async Task DoDispose()
        {
            //unregiste from register center
            await Task.CompletedTask;
            _server.Stop();
            Logger().LogInformation("stoped the thrift server ...");
        }
    }
}
