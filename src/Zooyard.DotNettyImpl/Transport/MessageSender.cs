using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Net;
using System;
using Zooyard.DotNettyImpl.Messages;
using System.Threading.Channels;
using Zooyard.DotNettyImpl.Transport.Codec;

namespace Zooyard.DotNettyImpl.Transport;

/// <summary>
/// 基于DotNetty的消息发送者基类。
/// </summary>
public abstract class DotNettyMessageSender
{
    private readonly ITransportMessageEncoder _transportMessageEncoder;
    public DotNettyMessageSender(ITransportMessageEncoder transportMessageEncoder) 
    {
        _transportMessageEncoder = transportMessageEncoder;
    } 
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
public class DotNettyMessageClientSender : DotNettyMessageSender, IMessageSender, IDisposable
{
    private readonly IChannel _channel;

    public DotNettyMessageClientSender(
        ITransportMessageEncoder transportMessageEncoder,
        IChannel channel) 
        : base(transportMessageEncoder)
    {
        _channel = channel;
    }
    public async Task Open(URL url)
    {
        if (!_channel.Open)
        {
            var host = new IPEndPoint(IPAddress.Parse(url.Host), url.Port);
            await _channel.ConnectAsync(host);
        }
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
public class DotNettyServerMessageSender : DotNettyMessageSender, IMessageSender
{
    private readonly IChannelHandlerContext _context;

    public DotNettyServerMessageSender(
        ITransportMessageEncoder transportMessageEncoder,
        IChannelHandlerContext context)
        : base(transportMessageEncoder)
    {
        _context = context;
    }
    public async Task Open(URL url)
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