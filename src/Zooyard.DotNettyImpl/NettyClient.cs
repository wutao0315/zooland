using DotNetty.Transport.Channels;
using System.Net;
using System;
using Zooyard.Logging;
using Zooyard.Rpc.Support;
using System.Threading.Channels;
using Zooyard.DotNettyImpl.Transport;

namespace Zooyard.DotNettyImpl;

public class NettyClient : AbstractClient
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyClient));
    //public const string QUIETPERIOD_KEY = "quietPeriod";
    //public const int DEFAULT_QUIETPERIOD = 100;
    //public const string TIMEOUT_KEY = "timeout";
    //public const int DEFAULT_TIMEOUT = 5000;
    public override string System => "zy_netty";
    public override URL Url { get; }
    public override int ClientTimeout { get; }
    
    private readonly ITransportClient _channel;

    public NettyClient(ITransportClient channel, int clientTimeout, URL url)
    {
        _channel = channel;
        this.ClientTimeout = clientTimeout;
        this.Url = url;
    }


    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        await this.Open(cancellationToken);

        return new NettyInvoker(_channel, this.ClientTimeout);
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
