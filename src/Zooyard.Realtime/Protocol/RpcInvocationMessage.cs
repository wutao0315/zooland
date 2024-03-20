namespace Zooyard.Realtime.Protocol;

/// <summary>
/// A base class for hub messages related to a specific invocation.
/// </summary>
public abstract record RpcInvocationMessage : RpcMessage
{
    /// <summary>
    /// Gets or sets a name/value collection of headers.
    /// </summary>
    public IDictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets the invocation ID.
    /// </summary>
    public string? InvocationId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcInvocationMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation ID.</param>
    protected RpcInvocationMessage(string? invocationId)
    {
        InvocationId = invocationId;
    }
}
