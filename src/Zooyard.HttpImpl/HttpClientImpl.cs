using Microsoft.Extensions.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.HttpImpl;

public class HttpClientImpl(ILogger<HttpClientImpl> _logger, IHttpClientFactory _transport, int clientTimeout, URL url) 
    : AbstractClient(clientTimeout, url)
{
    public override string System => "zy_http";

    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        var client = _transport.CreateClient();
        var result = await client.SendAsync(new HttpRequestMessage
        {
            Method = new HttpMethod("GET"),
            RequestUri = new Uri($"{this.Url.Protocol}://{this.Url.Host}:{this.Url.Port}/health"),
        }, cancellationToken);

        result.EnsureSuccessStatusCode();

        return new HttpInvoker(_logger, _transport, ClientTimeout, Url);
    }
    public override async Task Open(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task Close(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 重置，连接归还连接池前操作
    /// </summary>
    public override void Reset()
    {
        //_transport.DefaultRequestHeaders.Clear();
        //_transport.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.93 Safari/537.36");
        //_transport.DefaultRequestHeaders.Connection.TryParseAdd("Keep-Alive");
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }
}
