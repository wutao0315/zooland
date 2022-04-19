using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace Zooyard.Rpc.NettyImpl;

internal class HeliosBackwardsCompatabilityLengthFramePrepender : LengthFieldPrepender
{
    private readonly List<object> _temporaryOutput = new (2);

    public HeliosBackwardsCompatabilityLengthFramePrepender(int lengthFieldLength,
        bool lengthFieldIncludesLengthFieldLength) : base(ByteOrder.LittleEndian, lengthFieldLength, 0, lengthFieldIncludesLengthFieldLength)
    {
    }

    protected override void Encode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
    {
        base.Encode(context, message, output);
        var lengthFrame = (IByteBuffer)_temporaryOutput[0];
        var combined = lengthFrame.WriteBytes(message);
        ReferenceCountUtil.SafeRelease(message, 1); // ready to release it - bytes have been copied
        output.Add(combined.Retain());
        _temporaryOutput.Clear();
    }
}
