namespace Zooyard.Realtime.Protocol;

/// <summary>
/// Represents the ID being acknowledged so older messages do not need to be buffered anymore.
/// </summary>
public sealed record AckMessage(long sequenceId) : RpcMessage
{

    /// <summary>
    /// The ID of the last message that was received.
    /// </summary>
    public long SequenceId { get; set; } = sequenceId;
}
