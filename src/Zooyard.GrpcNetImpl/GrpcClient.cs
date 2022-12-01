using Grpc.Core;
using Grpc.Net.Client;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.GrpcNetImpl;

public class GrpcClient : AbstractClient
{
    public override URL Url { get; }
    public override int ClientTimeout { get; }
    private GrpcChannel _channel;
    private readonly object _grpcClient;
    private readonly GrpcChannelOptions _grpcChannelOptions;
    public GrpcClient(GrpcChannel channel, 
        object grpcClient, 
        URL url, 
        GrpcChannelOptions grpcChannelOptions,
        int clientTimeout)
    {
        this.Url = url;
        this.ClientTimeout = clientTimeout;
        _channel = channel;
        _grpcChannelOptions = grpcChannelOptions;
        _grpcClient = grpcClient;
        
    }
   
    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        //if (_channel?.State == ConnectivityState.Shutdown)
        //{
        //    _channel = GrpcChannel.ForAddress(_channel.Target, _grpcChannelOptions);
        //}

        await Open(cancellationToken);
        //grpc client service

        return new GrpcInvoker(_grpcClient, this.ClientTimeout);
    }

    public override async Task Open(CancellationToken cancellationToken = default)
    {
        if (_channel.State != ConnectivityState.Ready)
        {
            await _channel.ConnectAsync(cancellationToken);
        }
        if (_channel.State != ConnectivityState.Ready)
        {
            throw new Grpc.Core.RpcException(Status.DefaultCancelled, "connect failed");
        }
    }

    public override async Task Close(CancellationToken cancellationToken = default)
    {
        if (_channel.State != ConnectivityState.Shutdown)
        {
            await _channel.ShutdownAsync();
        }
        _channel.Dispose();
    }
    public override async ValueTask DisposeAsync()
    {
        await Close();
    }
}
