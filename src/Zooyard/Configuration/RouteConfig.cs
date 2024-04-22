using Zooyard.Utils;

namespace Zooyard.Configuration;

public sealed record RouteConfig
{
    /// <summary>
    /// Globally unique identifier of the route config.
    /// This field is required.
    /// </summary>
    public string RouteId { get; init; } = string.Empty;
    /// <summary>
    /// used to match service name pattern. 
    /// 正则表达式
    /// This field is required.
    /// </summary>
    public string? ServicePattern { get; set; }

    /// <summary>
    /// Arbitrary key-value pairs that further describe this route.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Optionally, an order value for this route. Routes with lower numbers take precedence over higher numbers.
    /// </summary>
    public int? Order { get; init; }
    public bool Equals(RouteConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(RouteId, other.RouteId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ServicePattern, other.ServicePattern, StringComparison.OrdinalIgnoreCase)
            && CaseSensitiveEqualHelper.Equals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        // HashCode.Combine(...) takes only 8 arguments
        var hash = new HashCode();
        hash.Add(RouteId?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        hash.Add(ServicePattern?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        hash.Add(CaseSensitiveEqualHelper.GetHashCode(Metadata));
        return hash.ToHashCode();
    }

}
