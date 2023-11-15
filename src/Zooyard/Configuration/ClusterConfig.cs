using Zooyard.Utils;

namespace Zooyard.Configuration;

public sealed record ClusterConfig
{
    /// <summary>
    /// The Id for this cluster. This needs to be globally unique.
    /// This field is required.
    /// </summary>
    public string ClusterId { get; init; } = default!;
    /// <summary>
    /// The set of destinations associated with this cluster.
    /// </summary>
    public IReadOnlyDictionary<string, DestinationConfig>? Destinations { get; init; }
    /// <summary>
    /// Arbitrary key-value pairs that further describe this cluster.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public bool Equals(ClusterConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        return EqualsExcludingDestinations(other)
            && CollectionEqualityHelper.Equals(Destinations, other.Destinations);
    }

    internal bool EqualsExcludingDestinations(ClusterConfig other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(ClusterId, other.ClusterId, StringComparison.OrdinalIgnoreCase)
            && CaseSensitiveEqualHelper.Equals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            ClusterId?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            CollectionEqualityHelper.GetHashCode(Destinations),
            CaseSensitiveEqualHelper.GetHashCode(Metadata));
    }
}
