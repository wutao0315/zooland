using DotNetty.Transport.Channels;
using System;
using System.Net;
using System.Threading.Tasks;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class DotNettyClient : AbstractClient
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyClient));
        public const string TIMEOUT_KEY = "http_timeout";
        public const int DEFAULT_TIMEOUT = 5000;
        public override URL Url { get; }

        private readonly IChannel _channel;
        private readonly int _clientTimeout;


        public DotNettyClient(IChannel channel, URL url)
        {
            _channel = channel;
            _clientTimeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
            this.Url = url;
        }


        public override async Task<IInvoker> Refer()
        {
            await this.Open();

            return new DotNettyInvoker(_channel, _clientTimeout);
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
        }
    }
}
