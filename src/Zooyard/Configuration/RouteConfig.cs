using System.Text.RegularExpressions;
using Zooyard.Utils;

namespace Zooyard.Configuration;

public sealed record RouteConfig
{
    /// <summary>
    /// Globally unique identifier of the route.
    /// This field is required.
    /// </summary>
    public string RouteId { get; init; } = default!;
    /// <summary>
    /// Gets or sets the cluster that requests matching this route
    /// should be proxied to.
    /// </summary>
    public string? ClusterId { get; init; }
    /// <summary>
    /// Arbitrary key-value pairs that further describe this route.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public bool Equals(RouteConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(RouteId, other.RouteId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ClusterId, other.ClusterId, StringComparison.OrdinalIgnoreCase)
            && CaseSensitiveEqualHelper.Equals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        // HashCode.Combine(...) takes only 8 arguments
        var hash = new HashCode();
        HashCode.Combine(RouteId?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            ClusterId?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            CaseSensitiveEqualHelper.GetHashCode(Metadata));
        return hash.ToHashCode();
    }
}
