using System.Diagnostics;

namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Feature to access the <see cref="Activity"/> associated with a request.
/// </summary>
public interface IHttpActivityFeature
{
    /// <summary>
    /// Returns the <see cref="Activity"/> associated with the current request.
    /// </summary>
    Activity Activity { get; set; }
}
