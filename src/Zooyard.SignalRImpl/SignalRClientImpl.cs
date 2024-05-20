using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.SignalRImpl;

public class SignalRClientImpl(ILogger<SignalRClientImpl> _logger, HubConnection _transport, URL url) 
    : AbstractClient(url)
{
    public override string System => "zy_signalr";

    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        if (_transport.State == HubConnectionState.Disconnected)
        {
            await _transport.StartAsync(cancellationToken);
        }
        return new SignalRInvoker(_logger, _transport, ClientTimeout, Url);
    }
    public override async Task Open(CancellationToken cancellationToken = default)
    {
        if (_transport.State == HubConnectionState.Disconnected)
        {
            await _transport.StartAsync(cancellationToken);
        }
        await Task.CompletedTask;
    }

    public override async Task Close(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (_transport.State != HubConnectionState.Disconnected)
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
