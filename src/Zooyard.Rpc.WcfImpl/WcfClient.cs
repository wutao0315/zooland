﻿using Microsoft.Extensions.Logging;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
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
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WcfClient>();
        }

        public override async Task<IInvoker> Refer()
        {
            Open();
            return new WcfInvoker(_channel, _loggerFactory);
        }

        public override async Task Open()
        {
            if (_channel.State != CommunicationState.Opened &&
               _channel.State != CommunicationState.Opening)
            {
                _channel.Open();
                await Task.CompletedTask;
            }
        }


        public override async Task Close()
        {
            if (_channel.State != CommunicationState.Closed &&
                 _channel.State != CommunicationState.Closing)
            {
                try
                {
                    _channel.Close();
                    await Task.CompletedTask;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        public override async Task DisposeAsync()
        {
            if (_channel.State != CommunicationState.Closed &&
                 _channel.State != CommunicationState.Closing)
            {
                try
                {
                    _channel.Close();
                    _channel.Abort();
                    await Task.CompletedTask;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }


    }
}
