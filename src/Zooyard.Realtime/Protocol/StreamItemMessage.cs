namespace Zooyard.Realtime.Protocol;

public record StreamItemMessage : RpcInvocationMessage
{
    /// <summary>
    /// The single item from a stream.
    /// </summary>
    public object? Item { get; set; }

    /// <summary>
    /// Constructs a <see cref="StreamItemMessage"/>.
    /// </summary>
    /// <param name="invocationId">The ID of the stream.</param>
    /// <param name="item">An item from the stream.</param>
    public StreamItemMessage(string invocationId, object? item) : base(invocationId)
    {
        Item = item;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"StreamItem {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Item)}: {Item ?? "<<null>>"} }}";
    }
}
