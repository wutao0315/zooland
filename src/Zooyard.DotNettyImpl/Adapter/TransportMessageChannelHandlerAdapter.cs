using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.DotNettyImpl.Transport.Codec;

namespace Zooyard.DotNettyImpl.Adapter;

public class TransportMessageChannelHandlerAdapter(ITransportMessageDecoder _transportMessageDecoder) : ChannelHandlerAdapter
{
    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        var buffer = (IByteBuffer)message;
        var data = new byte[buffer.ReadableBytes];
        buffer.ReadBytes(data);
        var transportMessage = _transportMessageDecoder.Decode(data);
        //var transportMessage = data.Desrialize<TransportMessage>();
        context.FireChannelRead(transportMessage);
        ReferenceCountUtil.Release(buffer);
    }
}

