using System.Security.Claims;

namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// The user associated with the connection.
/// </summary>
public interface IConnectionUserFeature
{
    /// <summary>
    /// Gets or sets the user associated with the connection.
    /// </summary>
    ClaimsPrincipal? User { get; set; }
}
