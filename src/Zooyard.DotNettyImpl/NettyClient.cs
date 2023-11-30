using DotNetty.Transport.Channels;
using System.Net;
using System;
//using Zooyard.Logging;
using Zooyard.Rpc.Support;
using System.Threading.Channels;
using Zooyard.DotNettyImpl.Transport;
using Microsoft.Extensions.Logging;

namespace Zooyard.DotNettyImpl;

public class NettyClient(ILogger<NettyClient> _logger, ITransportClient _channel, int clientTimeout, URL _url)
    : AbstractClient(clientTimeout, _url)
{
    public override string System => "zy_netty";

    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        await this.Open(cancellationToken);

        return new NettyInvoker(_logger, _channel, ClientTimeout);
    }

    public override async Task Open(CancellationToken cancellationToken = default)
    {
        await _channel.Open(Url);
        await Task.CompletedTask;
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
