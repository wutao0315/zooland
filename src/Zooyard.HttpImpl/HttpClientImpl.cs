using Zooyard.Rpc.Support;

namespace Zooyard.HttpImpl;

public class HttpClientImpl : AbstractClient
{
    public override URL Url { get; }
    private readonly IHttpClientFactory _transport;
    private readonly int _clientTimeout;
    
    public HttpClientImpl(IHttpClientFactory transport,URL url,int clientTimeout)
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
        var client = _transport.CreateClient();
        var result = await client.SendAsync(new HttpRequestMessage
        {
            Method = new HttpMethod("GET"),
            RequestUri = new Uri($"{this.Url.Protocol}://{this.Url.Host}:{this.Url.Port}/health"),
        });

        result.EnsureSuccessStatusCode();
        isOpen[0] = true;

        return new HttpInvoker(_transport, _clientTimeout, Url, isOpen);
    }
    public override async Task Open()
    {
        await Task.CompletedTask;
    }

    public override async Task Close()
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
