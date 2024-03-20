namespace Zooyard.Realtime.Protocol;
/// <summary>
/// A base class for hub messages representing an invocation.
/// </summary>
public abstract record RpcMethodInvocationMessage : RpcInvocationMessage
{
    /// <summary>
    /// Gets the target method name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the target method arguments.
    /// </summary>
    public object?[] Arguments { get; }

    /// <summary>
    /// The target methods stream IDs.
    /// </summary>
    public string[]? StreamIds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcMethodInvocationMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation ID.</param>
    /// <param name="target">The target method name.</param>
    /// <param name="arguments">The target method arguments.</param>
    /// <param name="streamIds">The target methods stream IDs.</param>
    protected RpcMethodInvocationMessage(string? invocationId, string target, object?[] arguments, string[]? streamIds)
        : this(invocationId, target, arguments)
    {
        StreamIds = streamIds;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcMethodInvocationMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation ID.</param>
    /// <param name="target">The target method name.</param>
    /// <param name="arguments">The target method arguments.</param>
    protected RpcMethodInvocationMessage(string? invocationId, string target, object?[] arguments)
        : base(invocationId)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentNullException($"target:{target} is null or empty");
        }

        Target = target;
        Arguments = arguments;
    }
}

/// <summary>
/// A hub message representing a non-streaming invocation.
/// </summary>
public record InvocationMessage : RpcMethodInvocationMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvocationMessage"/> class.
    /// </summary>
    /// <param name="target">The target method name.</param>
    /// <param name="arguments">The target method arguments.</param>
    public InvocationMessage(string target, object?[] arguments)
        : this(null, target, arguments)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvocationMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation ID.</param>
    /// <param name="target">The target method name.</param>
    /// <param name="arguments">The target method arguments.</param>
    public InvocationMessage(string? invocationId, string target, object?[] arguments)
        : base(invocationId, target, arguments)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvocationMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation ID.</param>
    /// <param name="target">The target method name.</param>
    /// <param name="arguments">The target method arguments.</param>
    /// <param name="streamIds">The target methods stream IDs.</param>
    public InvocationMessage(string? invocationId, string target, object?[] arguments, string[]? streamIds)
        : base(invocationId, target, arguments, streamIds)
    {
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string args;
        string streamIds;
        try
        {
            args = Arguments == null ? string.Empty : string.Join(", ", Arguments.Select(a => a?.ToString()));
        }
        catch (Exception ex)
        {
            args = $"Error: {ex.Message}";
        }

        try
        {
            streamIds = string.Join(", ", StreamIds != null ? StreamIds.Select(id => id?.ToString()) : Array.Empty<string>());
        }
        catch (Exception ex)
        {
            streamIds = $"Error: {ex.Message}";
        }

        return $"InvocationMessage {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Target)}: \"{Target}\", {nameof(Arguments)}: [ {args} ], {nameof(StreamIds)}: [ {streamIds} ] }}";
    }
}

/// <summary>
/// A hub message representing a streaming invocation.
/// </summary>
public record StreamInvocationMessage : RpcMethodInvocationMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamInvocationMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation ID.</param>
    /// <param name="target">The target method name.</param>
    /// <param name="arguments">The target method arguments.</param>
    public StreamInvocationMessage(string invocationId, string target, object?[] arguments)
        : base(invocationId, target, arguments)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamInvocationMessage"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation ID.</param>
    /// <param name="target">The target method name.</param>
    /// <param name="arguments">The target method arguments.</param>
    /// <param name="streamIds">The target methods stream IDs.</param>
    public StreamInvocationMessage(string invocationId, string target, object?[] arguments, string[]? streamIds)
        : base(invocationId, target, arguments, streamIds)
    {
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string args;
        string streamIds;
        try
        {
            args = Arguments == null ? string.Empty : string.Join(", ", Arguments.Select(a => a?.ToString()));
        }
        catch (Exception ex)
        {
            args = $"Error: {ex.Message}";
        }

        try
        {
            streamIds = string.Join(", ", StreamIds != null ? StreamIds.Select(id => id?.ToString()) : Array.Empty<string>());
        }
        catch (Exception ex)
        {
            streamIds = $"Error: {ex.Message}";
        }

        return $"StreamInvocation {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Target)}: \"{Target}\", {nameof(Arguments)}: [ {args} ], {nameof(StreamIds)}: [ {streamIds} ] }}";
    }
}
