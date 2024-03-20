using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zooyard.Realtime.Protocol;

namespace Zooyard.Realtime.TextJson;

/// <summary>
/// Extension methods for <see cref="IRpcBuilder"/>.
/// </summary>
public static class JsonProtocolDependencyInjectionExtensions
{
    /// <summary>
    /// Enables the JSON protocol for SignalR.
    /// </summary>
    /// <remarks>
    /// This has no effect if the JSON protocol has already been enabled.
    /// </remarks>
    /// <param name="builder">The <see cref="IRpcBuilder"/> representing the SignalR server to add JSON protocol support to.</param>
    /// <returns>The value of <paramref name="builder"/></returns>
    public static TBuilder AddJsonProtocol<TBuilder>(this TBuilder builder) where TBuilder : IRpcBuilder
        => builder.AddJsonProtocol(_ => { });

    /// <summary>
    /// Enables the JSON protocol for SignalR and allows options for the JSON protocol to be configured.
    /// </summary>
    /// <remarks>
    /// Any options configured here will be applied, even if the JSON protocol has already been registered with the server.
    /// </remarks>
    /// <param name="builder">The <see cref="IRpcBuilder"/> representing the SignalR server to add JSON protocol support to.</param>
    /// <param name="configure">A delegate that can be used to configure the <see cref="TextJsonProtocolOptions"/></param>
    /// <returns>The value of <paramref name="builder"/></returns>
    public static TBuilder AddJsonProtocol<TBuilder>(this TBuilder builder, Action<TextJsonProtocolOptions> configure) where TBuilder : IRpcBuilder
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IRpcProtocol, TextJsonProtocol>());
        builder.Services.Configure(configure);
        return builder;
    }
}
