using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Zooyard.DotNettyImpl.Codec;
using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Transport;

/// <summary>
/// 基于DotNetty的消息发送者基类。
/// </summary>
public abstract class DotNettyMessageSender
{
    private readonly ITransportMessageEncoder _transportMessageEncoder;

    protected DotNettyMessageSender(ITransportMessageEncoder transportMessageEncoder)
    {
        _transportMessageEncoder = transportMessageEncoder;
    }

    protected IByteBuffer GetByteBuffer(TransportMessage message)
    {
        var data = _transportMessageEncoder.Encode(message);
        //var buffer = PooledByteBufferAllocator.Default.Buffer();
        return Unpooled.WrappedBuffer(data);
    }
}

/// <summary>
/// 基于DotNetty客户端的消息发送者。
/// </summary>
public class DotNettyMessageClientSender : DotNettyMessageSender, IMessageSender
{
    private readonly IChannel _channel;

    public DotNettyMessageClientSender(ITransportMessageEncoder transportMessageEncoder, IChannel channel) : base(transportMessageEncoder)
    {
        _channel = channel;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _channel.DisconnectAsync();
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
}