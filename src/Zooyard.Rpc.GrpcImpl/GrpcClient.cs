using Grpc.Core;
using Microsoft.Extensions.Logging;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcClient : AbstractClient
    {
        public override URL Url { get; }
        private Channel _channel;
        private readonly ChannelCredentials _channelCredentials;
        private readonly int _clientTimeout;
        private readonly object _grpcClient;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        public GrpcClient(Channel channel,object grpcClient, URL url, ChannelCredentials channelCredentials, int clientTimeout,ILoggerFactory loggerFactory)
        {
            this.Url = url;
            _channel = channel;
            _channelCredentials = channelCredentials;
            _grpcClient = grpcClient;
            _clientTimeout = clientTimeout;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<GrpcClient>();
        }
       



        public override IInvoker Refer()
        {
            if (_channel != null && _channel.State == ChannelState.Shutdown)
            {
                _channel = new Channel(_channel.Target, _channelCredentials);
            }

            Open();
            //grpc client service

            return new GrpcInvoker(_grpcClient, _clientTimeout,_loggerFactory);
        }

        public override void Open()
        {
            if (_channel.State != ChannelState.Ready)
            {
                _channel.ConnectAsync().Wait(_clientTimeout / 2);
            }
            if (_channel.State != ChannelState.Ready)
            {
                throw new Grpc.Core.RpcException(Status.DefaultCancelled, "connect failed");
            }
        }

        public override void Close()
        {
            if (_channel != null)
            {
                _channel.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        public override void Dispose()
        {
            if (_channel != null)
            {
                _channel.ShutdownAsync().GetAwaiter().GetResult();
            }
        }
    }
}
