using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Zooyard.Loader;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Compressor;
using Zooyard.Rpc.NettyImpl.Exceptions;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.NettyImpl.Serializer;

namespace Zooyard.Rpc.NettyImpl.Support.V1;

/// <summary>
/// <pre>
/// 0     1     2     3     4     5     6     7     8     9    10     11    12    13    14    15    16
/// +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
/// |   magic   |Proto|     Full length       |    Head   | Msg |Seria|Compr|     RequestId         |
/// |   code    |colVer|    (head+body)      |   Length  |Type |lizer|ess  |                       |
/// +-----------+-----------+-----------+-----------+-----------+-----------+-----------+-----------+
/// |                                                                                               |
/// |                                   Head Map [Optional]                                         |
/// +-----------+-----------+-----------+-----------+-----------+-----------+-----------+-----------+
/// |                                                                                               |
/// |                                         body                                                  |
/// |                                                                                               |
/// |                                        ... ...                                                |
/// +-----------------------------------------------------------------------------------------------+
/// </pre>
/// <para>
/// <li>Full Length: include all data </li>
/// <li>Head Length: include head data from magic code to head map. </li>
/// <li>Body Length: Full Length - Head Length</li>
/// </para>
/// </summary>
public class ProtocolV1Decoder : LengthFieldBasedFrameDecoder
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ProtocolV1Decoder));
    /// <summary>
    /// default is 8M
    /// </summary>
    public ProtocolV1Decoder(int maxFrameLength = ProtocolConstants.MAX_FRAME_LENGTH) 
        : base(maxFrameLength, 3, 4, -7, 0)
    {
        /*
        int maxFrameLength,      
        int lengthFieldOffset,  magic code is 2B, and version is 1B, and then FullLength. so value is 3
        int lengthFieldLength,  FullLength is int(4B). so values is 4
        int lengthAdjustment,   FullLength include all data and read 7 bytes before, so the left length is (FullLength-7). so values is -7
        int initialBytesToStrip we will check magic code and version self, so do not strip any bytes. so values is 0
        */
    }

    protected override object Decode(IChannelHandlerContext context, IByteBuffer input)
    {
        object decoded;
        try
        {
            decoded = base.Decode(context, input);
            if (decoded is IByteBuffer frame)
            {
                try
                {
                    return DecodeFrame(frame);
                }
                finally
                {
                    frame.Release();
                }
            }
        }
        catch (Exception exx)
        {
            Logger().LogError(exx, $"Decode frame error, cause: {exx.Message}");
            throw new DecodeException(exx);
        }
        
        return decoded;
    }


    public object DecodeFrame(IByteBuffer frame)
    {
        byte b0 = frame.ReadByte();
        byte b1 = frame.ReadByte();
        if (ProtocolConstants.MAGIC_CODE_BYTES[0] != b0
                || ProtocolConstants.MAGIC_CODE_BYTES[1] != b1)
        {

            throw new ArgumentException("Unknown magic code: " + b0 + ", " + b1);
        }

        byte version = frame.ReadByte();
        // TODO  check version compatible here

        int fullLength = frame.ReadInt();
        short headLength = frame.ReadShort();
        byte messageType = frame.ReadByte();
        byte codecType = frame.ReadByte();
        byte compressorType = frame.ReadByte();
        int requestId = frame.ReadInt();

        var rpcMessage = new RpcMessage
        {
            Id = requestId,
            Codec = codecType,
            Compressor = compressorType,
            MessageType = messageType
        };

        // direct read head with zero-copy
        int headMapLength = headLength - ProtocolConstants.V1_HEAD_LENGTH;
        if (headMapLength > 0)
        {
            var map = HeadMapSerializer.getInstance().Decode(frame, headMapLength);
            rpcMessage.HeadMap.PutAll(map);
        }

        // read body
        if (messageType == ProtocolConstants.MSGTYPE_HEARTBEAT_REQUEST)
        {
            rpcMessage.Body = HeartbeatMessage.PING;
        }
        else if (messageType == ProtocolConstants.MSGTYPE_HEARTBEAT_RESPONSE)
        {
            rpcMessage.Body = HeartbeatMessage.PONG;
        }
        else
        {
            int bodyLength = fullLength - headLength;
            if (bodyLength > 0)
            {
                byte[] bs = new byte[bodyLength];
                frame.ReadBytes(bs);
                ICompressor compressor = CompressorFactory.GetCompressor(compressorType);
                bs = compressor.Decompress(bs);
                ISerializer serializer = EnhancedServiceLoader.Load<ISerializer>(((SerializerType)rpcMessage.Codec).ToString());
                rpcMessage.Body = serializer.Deserialize<AbstractMessage>(bs);
            }
        }

        if (Logger().IsEnabled(LogLevel.Debug)) 
        {
            Logger().LogDebug(rpcMessage.ToString());
        }

        return rpcMessage;
    }
}
