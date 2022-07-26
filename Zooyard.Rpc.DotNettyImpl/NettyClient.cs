using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using System.Net;
using Zooyard.Logging;
using Zooyard.Rpc.DotNettyImpl.Messages;
using Zooyard.Rpc.DotNettyImpl.Transport;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.DotNettyImpl;

public class NettyClient : AbstractClient
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyClient));
    public const string QUIETPERIOD_KEY = "quietPeriod";
    public const int DEFAULT_QUIETPERIOD = 100;
    //public const string TIMEOUT_KEY = "timeout";
    //public const int DEFAULT_TIMEOUT = 5000;

    public override URL Url { get; }

    private readonly int _clientTimeout;
    private readonly ITransportClient _transportClient;

    public NettyClient(ITransportClient transportClient, int clientTimeout, URL url)
    {
        _transportClient = transportClient;
        _clientTimeout = clientTimeout;
        this.Url = url;
    }


    public override async Task<IInvoker> Refer()
    {
        await this.Open();

        return new NettyInvoker(_transportClient, _clientTimeout);
    }

    public override async Task Open()
    {
        //if (!_channel.Open || !_channel.Active)
        //{
        //    var k = new IPEndPoint(IPAddress.Parse(Url.Host), Url.Port);
        //    await _channel.ConnectAsync(k);
        //}
    }

    public override async Task Close()
    {
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
