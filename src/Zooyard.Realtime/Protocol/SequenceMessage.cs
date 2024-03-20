namespace Zooyard.Realtime.Protocol;
/// <summary>
/// Represents the restart of the sequence of messages being sent. <see cref="SequenceId"/> is the starting ID of messages being sent, which might be duplicate messages.
/// </summary>
public sealed record SequenceMessage(long sequenceId) : RpcMessage
{
    /// <summary>
    /// The new starting ID of incoming messages.
    /// </summary>
    public long SequenceId { get; set; } = sequenceId;
}
