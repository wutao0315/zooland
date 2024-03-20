using Microsoft.Extensions.DependencyInjection;

namespace Zooyard.Realtime.Connection;

/// <summary>
/// A builder abstraction for configuring <see cref="RpcConnection"/> instances.
/// </summary>
public interface IRpcConnectionBuilder : IRpcBuilder
{
    /// <summary>
    /// Creates a <see cref="RpcConnection"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="RpcConnection"/> built using the configured options.
    /// </returns>
    RpcConnection Build();
}
