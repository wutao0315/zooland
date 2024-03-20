namespace Zooyard.Realtime.Connection;

/// <summary>
/// The context passed to <see cref="IRetryPolicy.NextRetryDelay(RetryContext)"/> to help the policy determine
/// how long to wait before the next retry and whether there should be another retry at all.
/// </summary>
public sealed record RetryContext
{
    /// <summary>
    /// The number of consecutive failed retries so far.
    /// </summary>
    public long PreviousRetryCount { get; set; }

    /// <summary>
    /// The amount of time spent retrying so far.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// The error precipitating the current retry if any.
    /// </summary>
    public Exception? RetryReason { get; set; }
}
