namespace Zooyard.DotNettyImpl.Messages;

/// <summary>
/// 远程调用结果消息。
/// </summary>
public sealed record RemoteInvokeResultMessage
{
    /// <summary>
    /// 异常消息。
    /// </summary>
    public string Msg { get; set; } = string.Empty;

    /// <summary>
    /// 状态码
    /// </summary>
    public int Code { get; set; } = 0;
    /// <summary>
    /// 结果内容。
    /// </summary>
    public object? Data { get; set; }
}
