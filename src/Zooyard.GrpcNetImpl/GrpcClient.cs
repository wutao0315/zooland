using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
//using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.GrpcNetImpl;

public class GrpcClient : AbstractClient
{
    public override string System => "zy_grpc";
    public override URL Url { get; }
    public override int ClientTimeout { get; }
    private GrpcChannel _channel;
    private readonly ILogger _logger;
    private readonly object _grpcClient;
    private readonly GrpcChannelOptions _grpcChannelOptions;
    public GrpcClient(ILogger<GrpcClient> logger,
        GrpcChannel channel, 
        object grpcClient, 
        URL url, 
        GrpcChannelOptions grpcChannelOptions,
        int clientTimeout)
    {
        _logger = logger;
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

        return new GrpcInvoker(_logger, _grpcClient, this.ClientTimeout);
    }

    public override async Task Open(CancellationToken cancellationToken = default)
    {
        using var cts = new CancellationTokenSource(ClientTimeout);
        await Timeout(_channel.ConnectAsync(cts.Token), ClientTimeout, cts);

        async Task Timeout(Task task, int millisecondsDelay, CancellationTokenSource cts)
        {
            if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)).ConfigureAwait(false) == task)
                return;

            cts.Cancel();

            throw new TimeoutException($"{Url} connect timeout");
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
