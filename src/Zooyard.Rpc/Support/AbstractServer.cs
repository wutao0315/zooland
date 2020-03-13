using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractServer : IServer
    {
        private readonly ILogger _logger;
        public AbstractServer(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AbstractServer>();
        }
        /// <summary>
        /// 注册中心发现机制
        /// </summary>
        public IRegistryHost RegistryHost { get; set; }
        public string Address { get; set; }


        public async Task Export()
        {
            //first start the service provider
            await DoExport();
            _logger.LogInformation("Export");
            if (!string.IsNullOrWhiteSpace(Address))
            {
                var url = URL.valueOf(Address);
                //registe this provoder
                RegistryHost.RegisterService(url);
            }
        }

        public void Dispose()
        {
            if (!string.IsNullOrWhiteSpace(Address))
            {
                var url = URL.valueOf(Address);
                //first unregiste this provider
                RegistryHost.DeregisterService(url);
            }
            //them stop the provider
            DoDispose().ConfigureAwait(false);
            _logger.LogInformation("Dispose");
        }

        public abstract Task DoDispose();
        
        public abstract Task DoExport();
    }
}
