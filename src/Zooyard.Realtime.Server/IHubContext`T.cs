// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Zooyard.Realtime.Server;

/// <summary>
/// A context abstraction for a hub.
/// </summary>
public interface IHubContext<THub, T>
    where THub : Hub<T>
    where T : class
{
    /// <summary>
    /// Gets a <see cref="IHubClients{T}"/> that can be used to invoke methods on clients connected to the hub.
    /// </summary>
    IHubClients<T> Clients { get; }

    /// <summary>
    /// Gets a <see cref="IGroupManager"/> that can be used to add and remove connections to named groups.
    /// </summary>
    IGroupManager Groups { get; }
}
