using System;
using System.Net.Http;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpClientPool : AbstractClientPool
    {
        public const string TIMEOUT_KEY = "http_timeout";
        public const int DEFAULT_TIMEOUT = 5000;

        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpClientPool));
       
        protected override IClient CreateClient(URL url)
        {
            //实例化TheTransport
            //获得transport参数,用于反射实例化
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
            
            //实例化TheThriftClient
            var client = new HttpClient
            {
                Timeout =TimeSpan.FromMilliseconds(timeout)
            };
            client.BaseAddress = new Uri($"{url.Protocol}://{url.Host}:{url.Port}");


            return new HttpClientImpl(client, url,timeout);
        }
    }
}
