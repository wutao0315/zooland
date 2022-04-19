using System.Net;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl;

public class HttpClientPool : AbstractClientPool
{
    public const string TIMEOUT_KEY = "http_timeout";
    public const int DEFAULT_TIMEOUT = 5000;

    //private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpClientPool));
   
    protected override async Task<IClient> CreateClient(URL url)
    {
        //实例化TheTransport
        //获得transport参数,用于反射实例化
        var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);

        //实例化TheHttpClient
        var httpClientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None | DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        var client = new HttpClient(httpClientHandler)
        {
            Timeout =TimeSpan.FromMilliseconds(timeout)
        };
        client.BaseAddress = new Uri($"{url.Protocol}://{url.Host}:{url.Port}");


        return new HttpClientImpl(client, url,timeout);
    }
}
