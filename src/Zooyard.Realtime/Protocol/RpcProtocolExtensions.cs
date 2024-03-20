using Zooyard.Realtime.Internal;

namespace Zooyard.Realtime.Protocol;

/// <summary>
/// Extension methods for <see cref="IRpcProtocol"/>.
/// </summary>
public static class RpcProtocolExtensions
{
    /// <summary>
    /// Converts the specified <see cref="RpcMessage"/> to its serialized representation.
    /// </summary>
    /// <param name="hubProtocol">The hub protocol.</param>
    /// <param name="message">The message to convert to bytes.</param>
    /// <returns>The serialized representation of the specified message.</returns>
    public static byte[] GetMessageBytes(this IRpcProtocol hubProtocol, RpcMessage message)
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            hubProtocol.WriteMessage(message, writer);
            return writer.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }
}
