using System.Net;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl;

public class HttpClientPool : AbstractClientPool
{
    public const string TIMEOUT_KEY = "http_timeout";
    public const int DEFAULT_TIMEOUT = 5000;

    private readonly IHttpClientFactory _httpClientFactory;
    public HttpClientPool(IHttpClientFactory httpClientFactory) 
    {
        _httpClientFactory = httpClientFactory;
    }
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(HttpClientPool));

    protected override async Task<IClient> CreateClient(URL url)
    {
        await Task.CompletedTask;
        //实例化TheTransport
        //获得transport参数,用于反射实例化
        var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
        //var client = _httpClientFactory.CreateClient();
        //client.Timeout = TimeSpan.FromMilliseconds(timeout);
        //client.BaseAddress = new Uri($"{url.Protocol}://{url.Host}:{url.Port}");

        return new HttpClientImpl(_httpClientFactory, url, timeout);
    }
}
