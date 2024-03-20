using System.Security.Claims;

namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// The HTTP authentication feature.
/// </summary>
public interface IHttpAuthenticationFeature
{
    /// <summary>
    /// Gets or sets the <see cref="ClaimsPrincipal"/> associated with the HTTP request.
    /// </summary>
    ClaimsPrincipal? User { get; set; }
}

