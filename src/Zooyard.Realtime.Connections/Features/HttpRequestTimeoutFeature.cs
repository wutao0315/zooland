namespace Zooyard.Realtime.Connections.Features;

internal sealed class HttpRequestTimeoutFeature : IHttpRequestTimeoutFeature
{
    private readonly CancellationTokenSource _timeoutCancellationTokenSource;

    public HttpRequestTimeoutFeature(CancellationTokenSource timeoutCancellationTokenSource)
    {
        _timeoutCancellationTokenSource = timeoutCancellationTokenSource;
    }

    public CancellationToken RequestTimeoutToken => _timeoutCancellationTokenSource.Token;

    public void DisableTimeout()
    {
        _timeoutCancellationTokenSource.CancelAfter(Timeout.Infinite);
    }
}

