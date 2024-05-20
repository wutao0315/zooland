using Microsoft.Extensions.Logging;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.HttpImpl;

public class HttpClientPool(ILoggerFactory _loggerFactory, IHttpClientFactory _httpClientFactory) : AbstractClientPool(_loggerFactory.CreateLogger<HttpClientPool>())
{
    protected override async Task<IClient> CreateClient(URL url)
    {
        await Task.CompletedTask;
        //实例化TheTransport
        //获得transport参数,用于反射实例化

        return new HttpClientImpl(_loggerFactory.CreateLogger<HttpClientImpl>(), _httpClientFactory, url);
    }
}
