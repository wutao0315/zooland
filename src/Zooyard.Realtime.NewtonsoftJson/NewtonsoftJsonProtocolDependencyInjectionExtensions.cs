using Microsoft.Extensions.DependencyInjection.Extensions;
using Zooyard.Protocols.MessagePack;
using Zooyard.Protocols.MessagePack.Protocol;
using Zooyard.Realtime.Protocol;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IRpcBuilder"/>.
/// </summary>
public static class NewtonsoftJsonProtocolDependencyInjectionExtensions
{
    /// <summary>
    /// Enables the JSON protocol for SignalR.
    /// </summary>
    /// <remarks>
    /// This has no effect if the JSON protocol has already been enabled.
    /// </remarks>
    /// <param name="builder">The <see cref="IRpcBuilder"/> representing the SignalR server to add JSON protocol support to.</param>
    /// <returns>The value of <paramref name="builder"/></returns>
    public static TBuilder AddNewtonsoftJsonProtocol<TBuilder>(this TBuilder builder) where TBuilder : IRpcBuilder
        => AddNewtonsoftJsonProtocol(builder, _ => { });

    /// <summary>
    /// Enables the JSON protocol for SignalR and allows options for the JSON protocol to be configured.
    /// </summary>
    /// <remarks>
    /// Any options configured here will be applied, even if the JSON protocol has already been registered with the server.
    /// </remarks>
    /// <param name="builder">The <see cref="IRpcBuilder"/> representing the SignalR server to add JSON protocol support to.</param>
    /// <param name="configure">A delegate that can be used to configure the <see cref="NewtonsoftJsonRpcProtocolOptions"/></param>
    /// <returns>The value of <paramref name="builder"/></returns>
    public static TBuilder AddNewtonsoftJsonProtocol<TBuilder>(this TBuilder builder, Action<NewtonsoftJsonRpcProtocolOptions> configure) where TBuilder : IRpcBuilder
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IRpcProtocol, NewtonsoftJsonRpcProtocol>());
        builder.Services.Configure(configure);
        return builder;
    }
}
