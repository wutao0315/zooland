using Zooyard.Configuration;

namespace Zooyard.Model;

public sealed class DestinationModel
{
    /// <summary>
    /// Creates a new instance. This constructor is for tests and infrastructure, this type is normally constructed by
    /// the configuration loading infrastructure.
    /// </summary>
    public DestinationModel(DestinationConfig destination)
    {
        Config = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    /// <summary>
    /// This destination's configuration.
    /// </summary>
    public DestinationConfig Config { get; }

    internal bool HasChanged(DestinationConfig destination)
    {
        return Config != destination;
    }
}
