﻿namespace Zooyard.Realtime.Features;

/// <summary>
/// Represents the lifetime of the connection.
/// </summary>
public interface IConnectionLifetimeFeature
{
    /// <summary>
    /// Gets or sets the <see cref="CancellationToken"/> that is triggered when the connection is closed.
    /// </summary>
    CancellationToken ConnectionClosed { get; set; }

    /// <summary>
    /// Terminates the current connection.
    /// </summary>
    void Abort();
}
