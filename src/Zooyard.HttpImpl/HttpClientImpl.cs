using Microsoft.Extensions.Logging;
using Zooyard.Exceptions;
using Zooyard.Rpc.Support;
using Zooyard.Utils;

namespace Zooyard.HttpImpl;

public class HttpClientImpl(ILogger<HttpClientImpl> _logger, IHttpClientFactory _transport, URL url) 
    : AbstractClient(url)
{
    public override string System => "zy_http";

    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {

        var healthcheck = url.GetParameter("healthcheck", false);
        if (healthcheck)
        {
            var checkType = url.GetParameter("checktype", "ping");
            if (checkType.Equals("ping", StringComparison.OrdinalIgnoreCase))
            {
                var opencheckTimeout = url.GetParameter("checktimeout", 1000);
                var isPingSuccess = await NetUtil.Ping(url.Host, opencheckTimeout);
                if (!isPingSuccess)
                {
                    throw new FrameworkException($"{url.Host} ping fail");
                }
            }
            else
            {
                var uri = $"{Url.Protocol}://{Url.Host}:{Url.Port}/health";
                try
                {
                    var client = _transport.CreateClient();
                    var opencheckTimeout = url.GetParameter("checktimeout", 1000);
                    client.Timeout = TimeSpan.FromMicroseconds(opencheckTimeout);
                    var result = await client.SendAsync(new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(uri),
                    }, cancellationToken);

                    result.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    throw new FrameworkException(ex, $"health chek from {uri} error");
                }
            }
        }


        return new HttpInvoker(_logger, _transport, ClientTimeout, Url);
    }
    public override async Task Open(CancellationToken cancellationToken = default)
    {
        var opencheck = url.GetParameter("opencheck", false);
        if (opencheck)
        {
            var checkType = url.GetParameter("checktype", "ping");
            if (checkType.Equals("ping", StringComparison.OrdinalIgnoreCase))
            {
                var opencheckTimeout = url.GetParameter("checktimeout", 1000);
                var isPingSuccess = await NetUtil.Ping(url.Host, opencheckTimeout);
                if (!isPingSuccess)
                {
                    throw new FrameworkException($"{url.Host} ping fail");
                }
            }
            else
            {
                var uri = $"{Url.Protocol}://{Url.Host}:{Url.Port}/health";
                try
                {
                    var client = _transport.CreateClient();
                    var opencheckTimeout = url.GetParameter("checktimeout", 1000);
                    client.Timeout = TimeSpan.FromMicroseconds(opencheckTimeout);
                    var result = await client.SendAsync(new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(uri),
                    }, cancellationToken);

                    result.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    throw new FrameworkException(ex, $"health chek from {uri} error");
                }
            }
        }
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
