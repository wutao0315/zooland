﻿using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Transport;

/// <summary>
/// 一个抽象的发送者。
/// </summary>
public interface IMessageSender
{
    Task Open(URL url);
    ///// <summary>
    ///// 发送消息。
    ///// </summary>
    ///// <param name="message">消息内容。</param>
    ///// <returns>一个任务。</returns>
    //Task SendAsync(TransportMessage message);

    /// <summary>
    /// 发送消息并清空缓冲区。
    /// </summary>
    /// <param name="message">消息内容。</param>
    /// <returns>一个任务。</returns>
    Task SendAndFlushAsync(TransportMessage message);
}
