using Microsoft.Extensions.Logging;
using Zooyard.Realtime.Connection;
using Zooyard.Rpc.Support;

namespace Zooyard.WebSocketsImpl;

public class WebSocketClientImpl(ILogger<WebSocketClientImpl> _logger, RpcConnection _transport, int clientTimeout, URL url) 
    : AbstractClient(clientTimeout, url)
{
    public override string System => "zy_websocket";

    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        if (_transport.State == RpcConnectionState.Disconnected) 
        {
            await _transport.StartAsync(cancellationToken);
        }
        return new WebSocketInvoker(_logger, _transport, ClientTimeout, Url);
    }
    public override async Task Open(CancellationToken cancellationToken = default)
    {
        if (_transport.State == RpcConnectionState.Disconnected)
        {
            await _transport.StartAsync(cancellationToken);
        }
        await Task.CompletedTask;
    }

    public override async Task Close(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (_transport.State != RpcConnectionState.Disconnected)
        {
            await _transport.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 重置，连接归还连接池前操作
    /// </summary>
    public override void Reset()
    {
    }

    public override async ValueTask DisposeAsync()
    {
        await _transport.DisposeAsync();
    }
}
