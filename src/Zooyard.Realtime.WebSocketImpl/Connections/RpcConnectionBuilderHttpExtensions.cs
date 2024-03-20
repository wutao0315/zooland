using Microsoft.Extensions.DependencyInjection;
using Zooyard.Realtime.Connection;

namespace Zooyard.WebSocketsImpl.Connections;

/// <summary>
/// Extension methods for <see cref="IRpcConnectionBuilder"/>.
/// </summary>
public static class RpcConnectionBuilderHttpExtensions
{
    /// <summary>
    /// Configures the <see cref="RpcConnection" /> to use HTTP-based transports to connect to the specified URL.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="url">The URL the <see cref="WebSocketConnection"/> will use.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithUrl(this IRpcConnectionBuilder rpcConnectionBuilder, string url)
    {
        rpcConnectionBuilder.WithUrlCore(new Uri(url), null);
        return rpcConnectionBuilder;
    }

    /// <summary>
    /// Configures the <see cref="RpcConnection" /> to use HTTP-based transports to connect to the specified URL.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="url">The URL the <see cref="WebSocketConnection"/> will use.</param>
    /// <param name="configureHttpConnection">The delegate that configures the <see cref="WebSocketConnection"/>.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithUrl(this IRpcConnectionBuilder rpcConnectionBuilder, string url, Action<WebSocketConnectionOptions> configureHttpConnection)
    {
        rpcConnectionBuilder.WithUrlCore(new Uri(url), configureHttpConnection);
        return rpcConnectionBuilder;
    }
    /// <summary>
    /// Configures the <see cref="RpcConnection" /> to use HTTP-based transports to connect to the specified URL.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithUrl(this IRpcConnectionBuilder rpcConnectionBuilder, Uri url)
    {
        rpcConnectionBuilder.WithUrlCore(url, null);
        return rpcConnectionBuilder;
    }

    /// <summary>
    /// Configures the <see cref="RpcConnection" /> to use HTTP-based transports to connect to the specified URL.
    /// </summary>
    /// <param name="rpcConnectionBuilder">The <see cref="IRpcConnectionBuilder" /> to configure.</param>
    /// <param name="url">The URL the <see cref="WebSocketConnection"/> will use.</param>
    /// <param name="configureHttpConnection">The delegate that configures the <see cref="WebSocketConnection"/>.</param>
    /// <returns>The same instance of the <see cref="IRpcConnectionBuilder"/> for chaining.</returns>
    public static IRpcConnectionBuilder WithUrl(this IRpcConnectionBuilder rpcConnectionBuilder, Uri url, Action<WebSocketConnectionOptions> configureHttpConnection)
    {
        rpcConnectionBuilder.WithUrlCore(url, configureHttpConnection);
        return rpcConnectionBuilder;
    }

    private static IRpcConnectionBuilder WithUrlCore(this IRpcConnectionBuilder rpcConnectionBuilder, Uri url, Action<WebSocketConnectionOptions>? configureHttpConnection)
    {
        if (rpcConnectionBuilder == null)
        {
            throw new ArgumentNullException(nameof(rpcConnectionBuilder));
        }

        rpcConnectionBuilder.Services.Configure<WebSocketConnectionOptions>(o =>
        {
            o.Url = url;
            //if (transports != null)
            //{
            //    o.Transports = transports.Value;
            //}
        });

        if (configureHttpConnection != null)
        {
            rpcConnectionBuilder.Services.Configure(configureHttpConnection);
        }

        rpcConnectionBuilder.Services.AddSingleton<IClientConnectionFactory, WebSocketConnectionFactory>();
        return rpcConnectionBuilder;
    }
}
