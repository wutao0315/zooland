﻿namespace Zooyard.Rpc.DotNettyImpl.Messages;

/// <summary>
/// 远程调用结果消息。
/// </summary>
[Serializable]
public class RemoteInvokeResultMessage
{
    /// <summary>
    /// 异常消息。
    /// </summary>
    public string ExceptionMessage { get; set; } = string.Empty;

    /// <summary>
    /// 状态码
    /// </summary>
    public int StatusCode { get; set; } = 200;
    /// <summary>
    /// 结果内容。
    /// </summary>
    public object? Result { get; set; }
}
