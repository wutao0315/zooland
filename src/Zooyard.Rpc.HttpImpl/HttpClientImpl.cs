using System;
using System.Net.Http;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpClientImpl : AbstractClient
    {
        public override URL Url { get; }
        /// <summary>
        /// 传输层
        /// </summary>
        public HttpClient TheTransport { get; set; }
        public HttpClientImpl(HttpClient client,URL url,int clientTimeout)
        {
            this.TheTransport = client;
            this.Url = url;
            this.ClientTimeout = clientTimeout;
        }
        
        public int ClientTimeout { get; set; }

        /// <summary>
        /// 开启标志
        /// </summary>
        protected bool[] isOpen = new bool[] { false };


        public override IInvoker Refer()
        {
            var task = TheTransport.SendAsync(new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri($"{this.Url.Protocol}://{this.Url.Host}:{this.Url.Port}/{this.Url.Path}/head")
            });

            if (task.Wait(this.ClientTimeout / 2))
            {
                task.Result.EnsureSuccessStatusCode();
                isOpen[0] = true;
            }
            else
            {
                isOpen[0] = false;
                throw new TimeoutException("连接时间超过" + this.ClientTimeout + "毫秒");
            }


            //grpc client service

            return new HttpInvoker(TheTransport,Url, isOpen);
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
            TheTransport.DefaultRequestHeaders.Clear();
            TheTransport.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.93 Safari/537.36");
            TheTransport.DefaultRequestHeaders.Connection.TryParseAdd("Keep-Alive");
        }

        public override void Dispose()
        {
            if (TheTransport != null)
            {
                TheTransport.Dispose();
            }
        }
    }
}
