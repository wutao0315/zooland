using Zooyard.Logging;

namespace Zooyard.Rpc.Support;

public abstract class AbstractServer : IServer
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(AbstractServer));
    /// <summary>
    /// 注册中心发现机制
    /// </summary>
    private readonly IRegistryService _registryService;
    public AbstractServer(IRegistryService registryService)
    {
        _registryService = registryService;
    }
    public string Address { get; set; }


    public async Task Export(CancellationToken cancellationToken)
    {
        //first start the service provider
        await DoExport(cancellationToken);
        Logger().LogInformation("Export");
        if (_registryService != null && !string.IsNullOrWhiteSpace(Address))
        {
            //registe this provoder
            var url = URL.ValueOf(Address);
            await _registryService.RegisterService(url);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrWhiteSpace(Address))
        {
            var url = URL.ValueOf(Address);
            //first unregiste this provider
            await _registryService.UnregisterService(url);
        }
        //them stop the provider
        await DoDispose();
        Logger().LogInformation("Dispose");
    }

    public abstract Task DoDispose();
    
    public abstract Task DoExport(CancellationToken cancellationToken);
}
