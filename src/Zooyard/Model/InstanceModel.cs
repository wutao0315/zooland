using Zooyard.Configuration;

namespace Zooyard.Model;

/// <summary>
/// Creates a new instance. This constructor is for tests and infrastructure, this type is normally constructed by
/// the configuration loading infrastructure.
/// </summary>
public sealed class InstanceModel(InstanceConfig instance)
{
    /// <summary>
    /// This destination's configuration.
    /// </summary>
    public InstanceConfig Config { get; } = instance ?? throw new ArgumentNullException(nameof(instance));

    internal bool HasChanged(InstanceConfig destination)
    {
        return Config != destination;
    }
}
