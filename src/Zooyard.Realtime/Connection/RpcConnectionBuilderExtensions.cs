using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zooyard.Realtime.Connection.Internal;

namespace Zooyard.Realtime.Connection;

/// <summary>
/// Extension methods for <see cref="IRpcConnectionBuilder"/>.
/// </summary>
public static class RpcConnectionBuilderExtensions
{
    /// <summary>
    /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder ConfigureLogging(this IRpcConnectionBuilder rpcConnectionBuilder, Action<ILoggingBuilder> configureLogging)
    {
        rpcConnectionBuilder.Services.AddLogging(configureLogging);
        return rpcConnectionBuilder;
    }

    /// <summary>
    /// Configures the <see cref="RpcConnection"/> to automatically attempt to reconnect if the connection is lost.
    /// The client will wait the default 0, 2, 10 and 30 seconds respectively before trying up to four reconnect attempts.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithAutomaticReconnect(this IRpcConnectionBuilder rpcConnectionBuilder)
    {
        rpcConnectionBuilder.Services.AddSingleton<IRetryPolicy>(new DefaultRetryPolicy());
        return rpcConnectionBuilder;
    }

    /// <summary>
    /// Configures the <see cref="RpcConnection"/> to automatically attempt to reconnect if the connection is lost.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="reconnectDelays">
    /// An array containing the delays before trying each reconnect attempt.
    /// The length of the array represents how many failed reconnect attempts it takes before the client will stop attempting to reconnect.
    /// </param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithAutomaticReconnect(this IRpcConnectionBuilder rpcConnectionBuilder, TimeSpan[] reconnectDelays)
    {
        rpcConnectionBuilder.Services.AddSingleton<IRetryPolicy>(new DefaultRetryPolicy(reconnectDelays));
        return rpcConnectionBuilder;
    }

    /// <summary>
    /// Configures the <see cref="RpcConnection"/> to automatically attempt to reconnect if the connection is lost.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="retryPolicy">An <see cref="IRetryPolicy"/> that controls the timing and number of reconnect attempts.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithAutomaticReconnect(this IRpcConnectionBuilder rpcConnectionBuilder, IRetryPolicy retryPolicy)
    {
        rpcConnectionBuilder.Services.AddSingleton(retryPolicy);
        return rpcConnectionBuilder;
    }

    /// <summary>
    /// Configures ServerTimeout for the <see cref="RpcConnection" />.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="timeout">ServerTimeout for the <see cref="RpcConnection"/>.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithServerTimeout(this IRpcConnectionBuilder rpcConnectionBuilder, TimeSpan timeout)
    {
        rpcConnectionBuilder.Services.Configure<RpcConnectionOptions>(o => o.ServerTimeout = timeout);
        return rpcConnectionBuilder;
    }

    /// <summary>
    /// Configures KeepAliveInterval for the <see cref="RpcConnection" />.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="interval">KeepAliveInterval for the <see cref="RpcConnection"/>.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithKeepAliveInterval(this IRpcConnectionBuilder rpcConnectionBuilder, TimeSpan interval)
    {
        rpcConnectionBuilder.Services.Configure<RpcConnectionOptions>(o => o.KeepAliveInterval = interval);
        return rpcConnectionBuilder;
    }
}