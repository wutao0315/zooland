using Zooyard.Utils;

namespace Zooyard.Configuration;

public sealed record ServiceConfig
{
    public string ServiceId { get; init; } = string.Empty;
    public IDictionary<string, InstanceConfig> Instances { get; init; } = new Dictionary<string, InstanceConfig>();
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public bool Equals(ServiceConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        return EqualsExcludingDestinations(other)
            && CollectionEqualityHelper.Equals(Instances, other.Instances);
    }

    internal bool EqualsExcludingDestinations(ServiceConfig other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(ServiceId, other.ServiceId, StringComparison.OrdinalIgnoreCase)
            && CaseSensitiveEqualHelper.Equals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            ServiceId?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            CollectionEqualityHelper.GetHashCode(Instances),
            CaseSensitiveEqualHelper.GetHashCode(Metadata));
    }
}
