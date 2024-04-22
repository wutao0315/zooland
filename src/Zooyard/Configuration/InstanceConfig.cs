using Zooyard.Utils;

namespace Zooyard.Configuration;

public sealed record InstanceConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
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
            Host.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Port.GetHashCode(),
            CaseSensitiveEqualHelper.GetHashCode(Metadata));
    }
}
