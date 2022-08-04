using Grpc.Core;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl;

public class GrpcClient : AbstractClient
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(GrpcClient));

    public override URL Url { get; }
    private Channel _channel;
    private readonly ChannelCredentials _channelCredentials;
    private readonly int _clientTimeout;
    private readonly object _grpcClient;
    public GrpcClient(Channel channel, 
        object grpcClient, 
        URL url, 
        ChannelCredentials channelCredentials,
        int clientTimeout)
    {
        this.Url = url;
        _channel = channel;
        _channelCredentials = channelCredentials;
        _grpcClient = grpcClient;
        _clientTimeout = clientTimeout;
    }
   
    public override async Task<IInvoker> Refer()
    {
        if (_channel?.State == ChannelState.Shutdown)
        {
            _channel = new Channel(_channel.Target, _channelCredentials);
        }

        await Open();
        //grpc client service

        return new GrpcInvoker(_grpcClient, _clientTimeout);
    }

    public override async Task Open()
    {
        if (_channel.State != ChannelState.Ready)
        {
            await _channel.ConnectAsync();//.Wait(_clientTimeout / 2);
        }
        if (_channel.State != ChannelState.Ready)
        {
            throw new Grpc.Core.RpcException(Status.DefaultCancelled, "connect failed");
        }
    }

    public override async Task Close()
    {
        if (_channel != null)
        {
            await _channel.ShutdownAsync();
        }
    }
    public override async ValueTask DisposeAsync()
    {
        await Close();
    }
}
