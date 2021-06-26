using System;
using System.Threading.Tasks;
using Thrift;
using Thrift.Transport;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftClient : AbstractClient
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ThriftClient));
        public override URL Url { get; }
        private readonly TBaseClient _thriftclient;
        private readonly int _clientTimeout;

        public ThriftClient(TBaseClient thriftclient, int clientTimeout, URL url)
        {
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
            if (!_thriftclient.OutputProtocol.Transport.IsOpen || !_thriftclient.InputProtocol.Transport.IsOpen)
            {
                //await _transport.OpenAsync();
                await _thriftclient.OpenTransportAsync();
            }
            Logger().LogInformation("open");
        }


        public override async Task Close()
        {
            _thriftclient.Dispose();
            Logger().LogInformation("close");
        }

        

        public override async ValueTask DisposeAsync()
        {
            await Close();
            Logger().LogInformation("Dispose");
        }
    }
}
