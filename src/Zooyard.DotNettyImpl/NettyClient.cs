using Microsoft.Extensions.Logging;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.Rpc.Support;

namespace Zooyard.DotNettyImpl;

public class NettyClient(ILogger<NettyClient> _logger, ITransportClient _channel, URL _url)
    : AbstractClient(_url)
{
    public override string System => "zy_netty";

    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        await this.Open(cancellationToken);

        return new NettyInvoker(_logger, _channel, ClientTimeout);
    }

    public override async Task Open(CancellationToken cancellationToken = default)
    {
        await _channel.Open(Url, cancellationToken);
    }

    public override async Task Close(CancellationToken cancellationToken = default)
    {
        _channel.Dispose();
        await Task.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        await Close();
    }
}
