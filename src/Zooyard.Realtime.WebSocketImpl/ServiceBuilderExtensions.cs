using Microsoft.Extensions.DependencyInjection;
using Zooyard.Realtime.Connection;
using Zooyard.WebSocketsImpl;
using Zooyard.WebSocketsImpl.Connections;

namespace Microsoft.Extensions.Configuration;

public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddWebSocket(this IRpcBuilder builder)
    {
        builder.Services.AddTransient<IClientConnectionFactory, WebSocketConnectionFactory>();
        builder.Services.AddTransient<WebSocketClientPool>();
        return builder;
    }
}
