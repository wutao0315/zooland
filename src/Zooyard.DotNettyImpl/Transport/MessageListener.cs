using DotNetty.Transport.Channels;
using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Transport;

/// <summary>
/// 接受到消息的委托。
/// </summary>
/// <param name="channel">消息发送者。</param>
/// <param name="message">接收到的消息。</param>
public delegate Task ReceivedDelegate(IMessageSender channel, TransportMessage? message);

/// <summary>
/// 消息监听者。
/// </summary>
public class MessageListener : IMessageListener
{
    /// <summary>
    /// 接收到消息的事件。
    /// </summary>
    public event ReceivedDelegate? Received;
    /// <summary>
    /// 触发接收到消息事件。
    /// </summary>
    /// <param name="channel">消息发送者。</param>
    /// <param name="message">接收到的消息。</param>
    /// <returns>一个任务。</returns>
    public async Task OnReceived(IMessageSender channel, TransportMessage? message)
    {
        if (Received == null)
            return;
        await Received(channel, message);
    }
}
