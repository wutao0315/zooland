﻿using System.IO.Pipelines;
using Zooyard.Realtime.Features;

namespace Zooyard.Realtime;


/// <summary>
/// Encapsulates all information about an individual connection.
/// </summary>
public abstract class ConnectionContext : BaseConnectionContext, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the <see cref="IDuplexPipe"/> that can be used to read or write data on this connection.
    /// </summary>
    public abstract IDuplexPipe Transport { get; set; }

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    /// <param name="abortReason">A <see cref="ConnectionAbortedException"/> describing the reason the connection is being terminated.</param>
    public override void Abort(ConnectionAbortedException abortReason)
    {
        // We expect this to be overridden, but this helps maintain back compat
        // with implementations of ConnectionContext that predate the addition of
        // ConnectionContext.Abort()
        Features.Get<IConnectionLifetimeFeature>()?.Abort();
    }

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    public override void Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via ConnectionContext.Abort()."));
}
