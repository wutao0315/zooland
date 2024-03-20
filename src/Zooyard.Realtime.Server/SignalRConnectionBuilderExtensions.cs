using System.Diagnostics.CodeAnalysis;
using Zooyard.Realtime.Server.Internal;

namespace Zooyard.Realtime.Server;

/// <summary>
/// </summary>
public static class SignalRConnectionBuilderExtensions
{
    //private const DynamicallyAccessedMemberTypes HubAccessibility = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods;

    ///// <summary>
    ///// Configure the connection to host the specified <see cref="Hub"/> type.
    ///// </summary>
    ///// <typeparam name="THub">The <see cref="Hub"/> type to host on the connection.</typeparam>
    ///// <param name="connectionBuilder">The connection to configure.</param>
    ///// <returns>The same instance of the <see cref="IConnectionBuilder"/> for chaining.</returns>
    //public static IConnectionBuilder UseHub<[DynamicallyAccessedMembers(HubAccessibility)] THub>(this IConnectionBuilder connectionBuilder) where THub : Hub
    //{
    //    var marker = connectionBuilder.ApplicationServices.GetService(typeof(SignalRCoreMarkerService));
    //    if (marker == null)
    //    {
    //        throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
    //            "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.");
    //    }

    //    return connectionBuilder.UseConnectionHandler<HubConnectionHandler<THub>>();
    //}
}
