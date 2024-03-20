using System.Net.Http.Headers;

namespace Zooyard.WebSocketsImpl.Connections.Internal;

internal class AccessTokenHttpMessageHandler : DelegatingHandler
{
    private readonly WebSocketConnection _httpConnection;

    public AccessTokenHttpMessageHandler(HttpMessageHandler inner, WebSocketConnection httpConnection) : base(inner)
    {
        _httpConnection = httpConnection;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _httpConnection.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
