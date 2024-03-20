﻿namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Provides access to the HTTP request lifetime operations.
/// </summary>
public interface IHttpRequestLifetimeFeature
{
    /// <summary>
    /// A <see cref="CancellationToken"/> that fires if the request is aborted and
    /// the application should cease processing. The token will not fire if the request
    /// completes successfully.
    /// </summary>
    CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Forcefully aborts the request if it has not already completed. This will result in
    /// RequestAborted being triggered.
    /// </summary>
    void Abort();
}

