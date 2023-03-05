namespace Zooyard.DotNettyImpl.Messages;

/// <summary>
/// 远程调用消息。
/// </summary>
public record RemoteInvokeMessage
{
    public string Method { get; set; } = string.Empty;
    public object[] Arguments { get; set; } = Array.Empty<object>();
    public Type[] ArgumentTypes { get; set; } = Array.Empty<Type>();
}
