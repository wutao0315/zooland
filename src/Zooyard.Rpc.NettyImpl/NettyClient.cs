using DotNetty.Transport.Channels;
using System;
using System.Net;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyClient : AbstractClient
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyClient));

        public const string QUIETPERIOD_KEY = "quietPeriod";
        public const int DEFAULT_QUIETPERIOD = 100;
        //public const string TIMEOUT_KEY = "timeout";
        //public const int DEFAULT_TIMEOUT = 5000;

        public override URL Url { get; }

        private readonly IEventLoopGroup _eventLoopGroup;
        private readonly IChannel _channel;
        private readonly IMessageListener _messageListener;
        private readonly int _clientTimeout;

        // private static readonly AttributeKey<EndPoint> origEndPointKey = AttributeKey<EndPoint>.ValueOf(typeof(NettyClientPool), nameof(EndPoint));

        public NettyClient(IEventLoopGroup eventLoopGroup, IChannel channel,IMessageListener messageListener, int clientTimeout, URL url)
        {
            _eventLoopGroup = eventLoopGroup;
            _channel = channel;
            _messageListener = messageListener;
            _clientTimeout = clientTimeout;
            this.Url = url;
        }


        public override async Task<IInvoker> Refer()
        {
            await this.Open();

            return new NettyInvoker(_channel, _messageListener, _clientTimeout);
        }

        public override async Task Open()
        {
            if (!_channel.Open || !_channel.Active)
            {
                var k = new IPEndPoint(IPAddress.Parse(Url.Host), Url.Port);
                await _channel.ConnectAsync(k);
            }
        }

        public override async Task Close()
        {
            if (_channel.Active || _channel.Open)
            {
                await _channel.CloseAsync();
            }
        }

        public override async ValueTask DisposeAsync()
        {
            await Close();
            await _channel.CloseAsync();
            if (!_eventLoopGroup.IsShutdown || !_eventLoopGroup.IsTerminated)
            {
                var quietPeriod = Url.GetParameter(QUIETPERIOD_KEY, DEFAULT_QUIETPERIOD);
                //var timeout = Url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
                await _eventLoopGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(quietPeriod), TimeSpan.FromMilliseconds(_clientTimeout));
            }
        }
    }
}
