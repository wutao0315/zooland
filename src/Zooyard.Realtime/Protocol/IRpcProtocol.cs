using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Zooyard.Realtime.Protocol;

/// <summary>
/// A protocol abstraction for communicating with SignalR hubs.
/// </summary>
public interface IRpcProtocol
{
    /// <summary>
    /// Gets the name of the protocol. The name is used by SignalR to resolve the protocol between the client and server.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the major version of the protocol.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the transfer format of the protocol.
    /// </summary>
    TransferFormat TransferFormat { get; }

    /// <summary>
    /// Creates a new <see cref="RpcMessage"/> from the specified serialized representation, and using the specified binder.
    /// </summary>
    /// <param name="input">The serialized representation of the message.</param>
    /// <param name="binder">The binder used to parse the message.</param>
    /// <param name="message">When this method returns <c>true</c>, contains the parsed message.</param>
    /// <returns>A value that is <c>true</c> if the <see cref="RpcMessage"/> was successfully parsed; otherwise, <c>false</c>.</returns>
    bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out RpcMessage? message);

    /// <summary>
    /// Writes the specified <see cref="RpcMessage"/> to a writer.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="output">The output writer.</param>
    void WriteMessage(RpcMessage message, IBufferWriter<byte> output);

    /// <summary>
    /// Converts the specified <see cref="RpcMessage"/> to its serialized representation.
    /// </summary>
    /// <param name="message">The message to convert.</param>
    /// <returns>The serialized representation of the message.</returns>
    ReadOnlyMemory<byte> GetMessageBytes(RpcMessage message);

    /// <summary>
    /// Gets a value indicating whether the protocol supports the specified version.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>A value indicating whether the protocol supports the specified version.</returns>
    bool IsVersionSupported(int version);
}
