using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Net;
using System.Threading.Channels;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.Exceptions;

namespace Zooyard.DotNettyImpl.Transport;

/// <summary>
/// 基于DotNetty的消息发送者基类。
/// </summary>
public abstract class DotNettyMessageSender(ITransportMessageEncoder _transportMessageEncoder)
{
    protected IByteBuffer GetByteBuffer(TransportMessage message)
    {
        var data = _transportMessageEncoder.Encode(message);
        //var buffer = PooledByteBufferAllocator.Default.Buffer();
        //var data = message.ToJsonBytes();
        return Unpooled.WrappedBuffer(data);
    }
}

/// <summary>
/// 基于DotNetty客户端的消息发送者。
/// </summary>
public class DotNettyMessageClientSender(ITransportMessageEncoder transportMessageEncoder,
        IChannel _channel) : DotNettyMessageSender(transportMessageEncoder), IMessageSender, IDisposable
{
    public async Task Open(URL url, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        if (!_channel.Open)
        {
            throw new FrameworkException($"url {url} already closed");
        }
        //if (!_channel.Open)
        //{
        //    var host = new IPEndPoint(IPAddress.Parse(url.Host), url.Port);
        //    await _channel.ConnectAsync(host);
        //}
    }

    /// <summary>
    /// 发送消息。
    /// </summary>
    /// <param name="message">消息内容。</param>
    /// <returns>一个任务。</returns>
    public async Task SendAsync(TransportMessage message)
    {
        var buffer = GetByteBuffer(message);
        await _channel.WriteAndFlushAsync(buffer);
    }

    /// <summary>
    /// 发送消息并清空缓冲区。
    /// </summary>
    /// <param name="message">消息内容。</param>
    /// <returns>一个任务。</returns>
    public async Task SendAndFlushAsync(TransportMessage message)
    {
        var buffer = GetByteBuffer(message);
        await _channel.WriteAndFlushAsync(buffer);
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        Task.Run(async () =>
        {
            await _channel.DisconnectAsync();
        }).Wait();
    }
}

/// <summary>
/// 基于DotNetty服务端的消息发送者。
/// </summary>
public class DotNettyServerMessageSender(
        ITransportMessageEncoder transportMessageEncoder,
        IChannelHandlerContext _context) : DotNettyMessageSender(transportMessageEncoder), IMessageSender
{
    public async Task Open(URL url, CancellationToken cancellationToken)
    {
        if (!_context.Channel.Open)
        {
            var host = new IPEndPoint(IPAddress.Parse(url.Host), url.Port);
            await _context.Channel.ConnectAsync(host);
        }
    }

    ///// <summary>
    ///// 发送消息。
    ///// </summary>
    ///// <param name="message">消息内容。</param>
    ///// <returns>一个任务。</returns>
    //public async Task SendAsync(TransportMessage message)
    //{
    //    var buffer = GetByteBuffer(message);
    //    await _context.WriteAsync(buffer);
    //}

    /// <summary>
    /// 发送消息并清空缓冲区。
    /// </summary>
    /// <param name="message">消息内容。</param>
    /// <returns>一个任务。</returns>
    public async Task SendAndFlushAsync(TransportMessage message)
    {
        var buffer = GetByteBuffer(message);
        await _context.WriteAndFlushAsync(buffer);
    }

}