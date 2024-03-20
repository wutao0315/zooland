using System.IO.Pipelines;

namespace Zooyard.Realtime.Connections.Features;


/// <summary>
/// The transport for the connection.
/// </summary>
public interface IConnectionTransportFeature
{
    /// <summary>
    /// Gets or sets the transport for the connection.
    /// </summary>
    IDuplexPipe Transport { get; set; }
}
