// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Zooyard.Realtime.Server.Internal;

namespace Zooyard.Realtime.Server;

/// <summary>
/// A base class for a strongly typed SignalR hub.
/// </summary>
/// <typeparam name="T">The type of client.</typeparam>
public abstract class Hub<T> : Hub where T : class
{
    private IHubCallerClients<T>? _clients;

    /// <summary>
    /// Gets or sets a <typeparamref name="T"/> that can be used to invoke methods on the clients connected to this hub.
    /// </summary>
    public new IHubCallerClients<T> Clients
    {
        get
        {
            if (_clients == null)
            {
                _clients = new TypedHubClients<T>(base.Clients);
            }
            return _clients;
        }
        set => _clients = value;
    }
}
