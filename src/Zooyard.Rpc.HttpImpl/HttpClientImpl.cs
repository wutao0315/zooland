﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpClientImpl : AbstractClient
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpClientImpl));
        public override URL Url { get; }
        private readonly HttpClient _transport;
        private readonly int _clientTimeout;
        
        public HttpClientImpl(HttpClient transport,URL url,int clientTimeout)
        {
            this.Url = url;
            _transport = transport;
            _clientTimeout = clientTimeout;
        }
        
        

        /// <summary>
        /// 开启标志
        /// </summary>
        protected bool[] isOpen = new bool[] { false };


        public override async Task<IInvoker> Refer()
        {
            var result = await _transport.SendAsync(new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri($"{this.Url.Protocol}://{this.Url.Host}:{this.Url.Port}/{this.Url.Path}/head")
            });

            result.EnsureSuccessStatusCode();
            isOpen[0] = true;

            //if (task.Wait(_clientTimeout / 2))
            //{
                
            //}
            //else
            //{
            //    isOpen[0] = false;
            //    throw new TimeoutException($"connection time out in {_clientTimeout} ms");
            //}


            //grpc client service

            return new HttpInvoker(_transport, Url, isOpen);
        }
        public override async Task Open()
        {
        }

        public override async Task Close()
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

        public override async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
            if (_transport != null)
            {
                _transport.Dispose();
            }
        }
    }
}
