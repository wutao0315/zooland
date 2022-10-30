using Grpc.Core;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.GrpcImpl;

public class GrpcClient : AbstractClient
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(GrpcClient));

    public override URL Url { get; }
    public override int ClientTimeout { get; }
    private Channel _channel;
    private readonly ChannelCredentials _channelCredentials;
    private readonly object _grpcClient;
    public GrpcClient(Channel channel, 
        object grpcClient, 
        URL url, 
        ChannelCredentials channelCredentials,
        int clientTimeout)
    {
        this.Url = url;
        this.ClientTimeout = clientTimeout;
        _channel = channel;
        _channelCredentials = channelCredentials;
        _grpcClient = grpcClient;
        
    }
   
    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        if (_channel?.State == ChannelState.Shutdown)
        {
            _channel = new Channel(_channel.Target, _channelCredentials);
        }

        await Open(cancellationToken);
        //grpc client service

        return new GrpcInvoker(_grpcClient, this.ClientTimeout);
    }

    public override async Task Open(CancellationToken cancellationToken = default)
    {
        if (_channel.State != ChannelState.Ready)
        {
            await _channel.ConnectAsync(DateTime.Now.AddMilliseconds(this.ClientTimeout));//.Wait(_clientTimeout / 2);
        }
        if (_channel.State != ChannelState.Ready)
        {
            throw new Grpc.Core.RpcException(Status.DefaultCancelled, "connect failed");
        }
    }

    public override async Task Close(CancellationToken cancellationToken = default)
    {
        if (_channel != null && _channel.State != ChannelState.Shutdown)
        {
            await _channel.ShutdownAsync();
        }
    }
    public override async ValueTask DisposeAsync()
    {
        await Close();
    }
}
