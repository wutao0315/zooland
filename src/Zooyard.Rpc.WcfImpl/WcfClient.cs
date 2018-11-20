using Microsoft.Extensions.Logging;
using System;
using System.ServiceModel;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfClient : AbstractClient
    {
        public override URL Url { get; }
        private readonly ICommunicationObject _channel;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        public WcfClient(ICommunicationObject channel, URL url, ILoggerFactory loggerFactory)
        {
            this.Url = url;
            _channel = channel;
            _logger = loggerFactory.CreateLogger<WcfClient>();
        }

        public override IInvoker Refer()
        {
            Open();
            return new WcfInvoker(_channel, _loggerFactory);
        }

        public override void Open()
        {

            if (_channel.State != CommunicationState.Opened &&
               _channel.State != CommunicationState.Opening)
            {
                _channel.Open();
            }
        }

        public override void Close()
        {
            if (_channel.State != CommunicationState.Closed &&
                 _channel.State != CommunicationState.Closing)
            {
                try
                {
                    _channel.Close();
                }
                catch(Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        public override void Dispose()
        {
            if (_channel.State != CommunicationState.Closed &&
                 _channel.State != CommunicationState.Closing)
            {
                try
                {
                    _channel.Close();
                    _channel.Abort();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }


    }
}
