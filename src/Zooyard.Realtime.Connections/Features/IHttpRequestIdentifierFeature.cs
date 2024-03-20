namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Feature to uniquely identify a request.
/// </summary>
public interface IHttpRequestIdentifierFeature
{
    /// <summary>
    /// Gets or sets a value to uniquely identify a request.
    /// This can be used for logging and diagnostics.
    /// </summary>
    string TraceIdentifier { get; set; }
}
