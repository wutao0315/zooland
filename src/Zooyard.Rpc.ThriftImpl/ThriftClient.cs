using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift;
using Thrift.Server;
using Thrift.Transports;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftClient : AbstractClient
    {
        public override URL Url { get; }
        /// <summary>
        /// 传输层
        /// </summary>
        private readonly TClientTransport _transport;
        private readonly IDisposable _thriftclient;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public ThriftClient(TClientTransport transport, IDisposable thriftclient, URL url, ILoggerFactory loggerFactory)
        {
            _transport = transport;
            _thriftclient = thriftclient;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ThriftClient>();
            this.Url = url;
        }


        public override async Task<IInvoker> Refer()
        {
            await this.Open();
            //thrift client service
            return new ThriftInvoker(_thriftclient, _loggerFactory);
        }

        public override async Task Open()
        {
            if (_transport != null && !_transport.IsOpen)
            {
                await _transport.OpenAsync();
            }
            _logger.LogInformation("open");
        }


        public override async Task Close()
        {
            if (_transport != null && _transport.IsOpen)
            {
                _transport.Close();
                await Task.CompletedTask;
            }
            _logger.LogInformation("close");
        }


        public override async Task DisposeAsync()
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
            _logger.LogInformation("Dispose");
        }
        

        
    }
}
