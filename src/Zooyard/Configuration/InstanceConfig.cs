using Zooyard.Utils;

namespace Zooyard.Configuration;

public sealed record InstanceConfig
{
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string Host { get; init; } = default!;
    public int Port { get; init; }

    public bool Equals(InstanceConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase)
            && Port == other.Port
            && CaseSensitiveEqualHelper.Equals(Metadata, other.Metadata);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Host?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Port.GetHashCode(),
            CaseSensitiveEqualHelper.GetHashCode(Metadata));
    }
}
