using System.IO.Pipelines;

namespace Zooyard.Realtime.Connections.Features;

/// <summary>
/// Represents the HTTP request body as a <see cref="PipeReader"/>.
/// </summary>
public interface IRequestBodyPipeFeature
{
    /// <summary>
    /// Gets a <see cref="PipeReader"/> representing the request body, if any.
    /// </summary>
    PipeReader Reader { get; }
}
