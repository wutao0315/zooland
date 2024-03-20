namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Used to control timeouts on the current request.
/// </summary>
public interface IHttpRequestTimeoutFeature
{
    /// <summary>
    /// A <see cref="CancellationToken" /> that will trigger when the request times out.
    /// </summary>
    CancellationToken RequestTimeoutToken { get; }

    /// <summary>
    /// Disables the request timeout if it hasn't already expired. This does not
    /// trigger the <see cref="RequestTimeoutToken"/>.
    /// </summary>
    void DisableTimeout();
}
