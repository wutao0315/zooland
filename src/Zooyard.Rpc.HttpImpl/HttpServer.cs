using Microsoft.AspNetCore.Hosting;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl;

public class HttpServer : AbstractServer
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpServer));
    private readonly IWebHost _server;
    public HttpServer(IWebHost server, IRegistryService registryService)
        : base(registryService)
    {
        _server = server;
    }


    public override async Task DoExport(CancellationToken cancellationToken)
    {
        try
        {
            await _server.RunAsync(cancellationToken);
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
