using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Transport;

/// <summary>
/// 一个抽象的传输客户端。
/// </summary>
public interface ITransportClient: IDisposable
{
    Task Open(URL url);
    /// <summary>
    /// 发送消息。
    /// </summary>
    /// <param name="message">远程调用消息模型。</param>
    /// <returns>远程调用消息的传输消息。</returns>
    Task<RemoteInvokeResultMessage> SendAsync(RemoteInvokeMessage message, CancellationToken cancellationToken);
}
