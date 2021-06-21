using System;
using System.Threading.Tasks;
using Thrift.Transports;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftClient : AbstractClient
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ThriftClient));
        public override URL Url { get; }
        /// <summary>
        /// 传输层
        /// </summary>
        private readonly TClientTransport _transport;
        private readonly IDisposable _thriftclient;
        private readonly int _clientTimeout;

        public ThriftClient(TClientTransport transport, IDisposable thriftclient, int clientTimeout, URL url)
        {
            _transport = transport;
            _thriftclient = thriftclient;
            _clientTimeout = clientTimeout;
            this.Url = url;
        }


        public override async Task<IInvoker> Refer()
        {
            await this.Open();
            //thrift client service
            return new ThriftInvoker(_thriftclient, _clientTimeout);
        }

        public override async Task Open()
        {
            if (_transport != null && !_transport.IsOpen)
            {
                await _transport.OpenAsync();
            }
            Logger().LogInformation("open");
        }


        public override async Task Close()
        {
            if (_transport != null && _transport.IsOpen)
            {
                _transport.Close();
                await Task.CompletedTask;
            }
            Logger().LogInformation("close");
        }

        

        public override async ValueTask DisposeAsync()
        {
            if (_transport != null)
            {
                await Close();
                _transport.Dispose();
            }
            if (_thriftclient != null)
            {
                _thriftclient.Dispose();
            }
            Logger().LogInformation("Dispose");
        }
    }
}
