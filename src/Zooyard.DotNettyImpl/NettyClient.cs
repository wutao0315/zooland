using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using System.Net;
using Zooyard.Logging;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.Rpc.Support;

namespace Zooyard.DotNettyImpl;

public class NettyClient : AbstractClient
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyClient));
    public const string QUIETPERIOD_KEY = "quietPeriod";
    public const int DEFAULT_QUIETPERIOD = 100;
    //public const string TIMEOUT_KEY = "timeout";
    //public const int DEFAULT_TIMEOUT = 5000;

    public override URL Url { get; }
    public override int ClientTimeout { get; }
    
    private readonly ITransportClient _transportClient;

    public NettyClient(ITransportClient transportClient, int clientTimeout, URL url)
    {
        _transportClient = transportClient;
        this.ClientTimeout = clientTimeout;
        this.Url = url;
    }


    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        await this.Open(cancellationToken);

        return new NettyInvoker(_transportClient, this.ClientTimeout);
    }

    public override async Task Open(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        //if (!_channel.Open || !_channel.Active)
        //{
        //    var k = new IPEndPoint(IPAddress.Parse(Url.Host), Url.Port);
        //    await _channel.ConnectAsync(k);
        //}
    }

    public override async Task Close(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        //if (_channel.Active || _channel.Open)
        //{
        //    await _channel.CloseAsync();
        //}
    }

    public override async ValueTask DisposeAsync()
    {
        await Close();
        //await _channel.CloseAsync();
        //if (!_eventLoopGroup.IsShutdown || !_eventLoopGroup.IsTerminated)
        //{
        //    var quietPeriod = Url.GetParameter(QUIETPERIOD_KEY, DEFAULT_QUIETPERIOD);
        //    //var timeout = Url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
        //    await _eventLoopGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(quietPeriod), TimeSpan.FromMilliseconds(_clientTimeout));
        //}
    }
}
