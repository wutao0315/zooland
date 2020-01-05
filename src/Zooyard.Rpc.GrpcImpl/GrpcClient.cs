using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public GrpcClient(Channel channel, 
            object grpcClient, 
            URL url, 
            ChannelCredentials channelCredentials,
            int clientTimeout,
            ILoggerFactory loggerFactory)
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
            if (_channel?.State == ChannelState.Shutdown)
            {
                _channel = new Channel(_channel.Target, _channelCredentials);
            }
           

            Open();
            //grpc client service


            return new GrpcInvoker(_grpcClient, _clientTimeout, _loggerFactory);
        }

        public override void Open()
        {
            OpenAsync().ConfigureAwait(false);
        }

        public override async Task OpenAsync()
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

        public override void Close()
        {
            CloseAsync().ConfigureAwait(false);
        }

        public override async Task CloseAsync()
        {
            if (_channel != null)
            {
                await _channel.ShutdownAsync();
            }
        }

        public override void Dispose()
        {
            CloseAsync().ConfigureAwait(false);
        }
    }
}
