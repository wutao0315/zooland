using System.Security.Claims;

namespace Zooyard.Realtime.Connections.Features;


/// <summary>
/// Default implementation for <see cref="IHttpAuthenticationFeature"/>.
/// </summary>
public class HttpAuthenticationFeature : IHttpAuthenticationFeature
{
    /// <inheritdoc />
    public ClaimsPrincipal? User { get; set; }
}
