using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpClientImpl : AbstractClient
    {
        public override URL Url { get; }
        private readonly HttpClient _transport;
        private readonly int _clientTimeout;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        public HttpClientImpl(HttpClient transport,URL url,int clientTimeout, ILoggerFactory loggerFactory)
        {
            this.Url = url;
            _transport = transport;
            _clientTimeout = clientTimeout;
            _loggerFactory= loggerFactory;
            _logger = loggerFactory.CreateLogger<HttpClientImpl>();
        }
        
        

        /// <summary>
        /// 开启标志
        /// </summary>
        protected bool[] isOpen = new bool[] { false };


        public override IInvoker Refer()
        {
            var task = _transport.SendAsync(new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri($"{this.Url.Protocol}://{this.Url.Host}:{this.Url.Port}/{this.Url.Path}/head")
            });

            if (task.Wait(_clientTimeout / 2))
            {
                task.Result.EnsureSuccessStatusCode();
                isOpen[0] = true;
            }
            else
            {
                isOpen[0] = false;
                throw new TimeoutException($"connection time out in {_clientTimeout} ms");
            }


            //grpc client service

            return new HttpInvoker(_transport, Url, isOpen, _loggerFactory);
        }
        public override void Open()
        {
            
        }

        public override void Close()
        {
            
        }

        /// <summary>
        /// 重置，连接归还连接池前操作
        /// </summary>
        public override void Reset()
        {
            _transport.DefaultRequestHeaders.Clear();
            _transport.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.93 Safari/537.36");
            _transport.DefaultRequestHeaders.Connection.TryParseAdd("Keep-Alive");
        }

        public override void Dispose()
        {
            if (_transport != null)
            {
                _transport.Dispose();
            }
        }
    }
}
