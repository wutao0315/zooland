using Microsoft.Extensions.Logging;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.HttpImpl;

public class HttpClientPool(ILoggerFactory _loggerFactory, IHttpClientFactory _httpClientFactory) : AbstractClientPool(_loggerFactory.CreateLogger<HttpClientPool>())
{
    public const string TIMEOUT_KEY = "http_timeout";
    public const int DEFAULT_TIMEOUT = 10000;

    protected override async Task<IClient> CreateClient(URL url)
    {
        await Task.CompletedTask;
        //实例化TheTransport
        //获得transport参数,用于反射实例化
        var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);

        return new HttpClientImpl(_loggerFactory.CreateLogger<HttpClientImpl>(), _httpClientFactory, timeout, url);
    }
}
