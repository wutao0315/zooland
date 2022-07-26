namespace Zooyard.Rpc.DotNettyImpl.Messages;

/// <summary>
/// 远程调用消息。
/// </summary>
[Serializable]
public class RemoteInvokeMessage
{
    public string Method { get; set; } = string.Empty;
    public object[] Arguments { get; set; } = Array.Empty<object>();
}
