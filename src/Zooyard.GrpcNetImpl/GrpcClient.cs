using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Zooyard.Rpc.Support;
using Zooyard.Utils;

namespace Zooyard.GrpcNetImpl;

public class GrpcClient(ILogger<GrpcClient> _logger,
        GrpcChannel _channel,
        object _grpcClient,
        int clientTimeout,
        URL url) : AbstractClient(clientTimeout, url)
{
    public override string System => "zy_grpc";
   
    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        //if (_channel?.State == ConnectivityState.Shutdown)
        //{
        //    _channel = GrpcChannel.ForAddress(_channel.Target, _grpcChannelOptions);
        //}

        await Open(cancellationToken);

        //grpc client service
        return new GrpcInvoker(_logger, _grpcClient, this.ClientTimeout);
    }

    public override async Task Open(CancellationToken cancellationToken = default)
    {
        await _channel.ConnectAsync(cancellationToken);
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
