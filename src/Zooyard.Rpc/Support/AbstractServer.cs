using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractServer : IServer
    {
        private readonly ILogger _logger;
        /// <summary>
        /// 注册中心发现机制
        /// </summary>
        private readonly IRegistryService _registryService;
        public AbstractServer(IRegistryService registryService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AbstractServer>();
            _registryService = registryService;
        }
        public string Address { get; set; }


        public async Task Export()
        {
            //first start the service provider
            await DoExport();
            _logger.LogInformation("Export");
            if (!string.IsNullOrWhiteSpace(Address))
            {
                //registe this provoder
                var url = URL.valueOf(Address);
                await _registryService.RegisterService(url);
            }
        }

        public void Dispose()
        {
            if (!string.IsNullOrWhiteSpace(Address))
            {
                var url = URL.valueOf(Address);
                //first unregiste this provider
                _registryService.UnregisterService(url).GetAwaiter().GetResult();
            }
            //them stop the provider
            DoDispose().GetAwaiter().GetResult();
            _logger.LogInformation("Dispose");
        }

        public abstract Task DoDispose();
        
        public abstract Task DoExport();
    }
}
