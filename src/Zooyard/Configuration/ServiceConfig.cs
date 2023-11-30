using Zooyard.Utils;

namespace Zooyard.Configuration;

public sealed record ServiceConfig
{
    public string ServiceName { get; init; } = default!;
    public IReadOnlyDictionary<string, InstanceConfig> Instances { get; init; } = default!;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = default!;

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

        return string.Equals(ServiceName, other.ServiceName, StringComparison.OrdinalIgnoreCase)
            && CaseSensitiveEqualHelper.Equals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            ServiceName?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            CollectionEqualityHelper.GetHashCode(Instances),
            CaseSensitiveEqualHelper.GetHashCode(Metadata));
    }
}
