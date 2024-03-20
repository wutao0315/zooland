using Newtonsoft.Json;
using Zooyard.Protocols.MessagePack.Protocol;

namespace Zooyard.Protocols.MessagePack;

/// <summary>
/// Options used to configure a <see cref="NewtonsoftJsonRpcProtocol"/> instance.
/// </summary>
public class NewtonsoftJsonRpcProtocolOptions
{
    /// <summary>
    /// Gets or sets the settings used to serialize invocation arguments and return values.
    /// </summary>
    public JsonSerializerSettings PayloadSerializerSettings { get; set; } = NewtonsoftJsonRpcProtocol.CreateDefaultSerializerSettings();
}
