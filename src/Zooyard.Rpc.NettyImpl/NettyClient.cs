using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyClient : AbstractClient
    {
        public const string QUIETPERIOD_KEY = "quietPeriod";
        public const int DEFAULT_QUIETPERIOD = 100;
        public const string TIMEOUT_KEY = "timeout";
        public const int DEFAULT_TIMEOUT = 1;

        public override URL Url { get; }

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IEventLoopGroup _eventLoopGroup;
        private readonly IChannel _channel;
        private readonly IMessageListener _messageListener;

        // private static readonly AttributeKey<EndPoint> origEndPointKey = AttributeKey<EndPoint>.ValueOf(typeof(NettyClientPool), nameof(EndPoint));

        public NettyClient(IEventLoopGroup eventLoopGroup, IChannel channel,IMessageListener messageListener ,URL url,ILoggerFactory loggerFactory)
        {
            this.Url = url;
            _eventLoopGroup = eventLoopGroup;
            _channel = channel;
            _messageListener = messageListener;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<NettyClient>();
        }


        public override IInvoker Refer()
        {
            this.Open();

            return new NettyInvoker(_channel, _messageListener, _loggerFactory);
        }

        public override void Open()
        {
            if (!_channel.Open || !_channel.Active)
            {
                var k = new IPEndPoint(IPAddress.Parse(Url.Host), Url.Port);
                _channel.ConnectAsync(k).GetAwaiter().GetResult();
            }
        }

        public override void Close()
        {
            if (_channel.Active || _channel.Open)
            {
                _channel.DisconnectAsync().GetAwaiter().GetResult();
            }
        }

        public override void Dispose()
        {
            Close();
            _channel.CloseAsync().GetAwaiter().GetResult();
            if (!_eventLoopGroup.IsShutdown || !_eventLoopGroup.IsTerminated)
            {
                var quietPeriod = Url.GetParameter(QUIETPERIOD_KEY, DEFAULT_QUIETPERIOD);
                var timeout = Url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
                _eventLoopGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(quietPeriod), TimeSpan.FromSeconds(timeout)).GetAwaiter().GetResult();
            }
        }
    }
}
