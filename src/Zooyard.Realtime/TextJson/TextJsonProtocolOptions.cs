using System.Text.Json;

namespace Zooyard.Realtime.TextJson;

/// <summary>
/// Options used to configure a <see cref="TextJsonProtocol"/> instance.
/// </summary>
public class TextJsonProtocolOptions
{
    /// <summary>
    /// Gets or sets the settings used to serialize invocation arguments and return values.
    /// </summary>
    public JsonSerializerOptions PayloadSerializerOptions { get; set; } = TextJsonProtocol.CreateDefaultSerializerSettings();
}
