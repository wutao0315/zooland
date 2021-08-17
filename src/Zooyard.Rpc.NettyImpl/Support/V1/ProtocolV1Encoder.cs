using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using Zooyard.Loader;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Compressor;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.NettyImpl.Serializer;

namespace Zooyard.Rpc.NettyImpl.Support.V1
{
    /// <summary>
    /// 0     1     2     3     4     5     6     7     8     9    10     11    12    13    14    15    16
    /// +-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+-----+
    /// |   magic   |Proto|     Full length       |    Head   | Msg |Seria|Compr|     RequestId         |
    /// |   code    |colVer|    （head+body)      |   Length  |Type |lizer|ess  |                       |
    /// +-----------+-----------+-----------+-----------+-----------+-----------+-----------+-----------+
    /// |                                                                                               |
    /// |                                   Head Map [Optional]                                         |
    /// +-----------+-----------+-----------+-----------+-----------+-----------+-----------+-----------+
    /// |                                                                                               |
    /// |                                         body                                                  |
    /// |                                                                                               |
    /// |                                        ... ...                                                |
    /// +-----------------------------------------------------------------------------------------------+
    /// <p>
    /// <li>Full Length: include all data </li>
    /// <li>Head Length: include head data from magic code to head map. </li>
    /// <li>Body Length: Full Length - Head Length</li>
    /// </p>
    /// https://github.com/seata/seata/issues/893
    /// 
    /// </summary>
    public class ProtocolV1Encoder : MessageToByteEncoder<object>
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ProtocolV1Encoder));

        protected override void Encode(IChannelHandlerContext context, object msg, IByteBuffer output)
        {
            try
            {
                if (msg is RpcMessage rpcMessage)
                {
                    int fullLength = ProtocolConstants.V1_HEAD_LENGTH;
                    int headLength = ProtocolConstants.V1_HEAD_LENGTH;

                    byte messageType = rpcMessage.MessageType;
                    output.WriteBytes(ProtocolConstants.MAGIC_CODE_BYTES);
                    output.WriteByte(ProtocolConstants.VERSION);
                    // full Length(4B) and head length(2B) will fix in the end. 
                    output.SetWriterIndex(output.WriterIndex + 6);
                    output.WriteByte(messageType);
                    output.WriteByte(rpcMessage.Codec);
                    output.WriteByte(rpcMessage.Compressor);
                    output.WriteInt(rpcMessage.Id);

                    // direct write head with zero-copy
                    IDictionary<string, string> headMap = rpcMessage.HeadMap;
                    if (headMap?.Count > 0)
                    {
                        int headMapBytesLength = HeadMapSerializer.getInstance().Encode(headMap, output);
                        headLength += headMapBytesLength;
                        fullLength += headMapBytesLength;
                    }

                    byte[] bodyBytes = null;
                    if (messageType != ProtocolConstants.MSGTYPE_HEARTBEAT_REQUEST
                            && messageType != ProtocolConstants.MSGTYPE_HEARTBEAT_RESPONSE)
                    {
                        ISerializer serializer = EnhancedServiceLoader.Load<ISerializer>(((SerializerType)rpcMessage.Codec).ToString());
                        bodyBytes = serializer.Serialize(rpcMessage.Body as AbstractMessage);
                        ICompressor compressor = CompressorFactory.GetCompressor(rpcMessage.Compressor);
                        bodyBytes = compressor.Compress(bodyBytes);
                        fullLength += bodyBytes.Length;
                    }

                    if (bodyBytes != null)
                    {
                        output.WriteBytes(bodyBytes);
                    }

                    // fix fullLength and headLength
                    int writeIndex = output.WriterIndex;
                    // skip magic code(2B) + version(1B)
                    output.SetWriterIndex(writeIndex - fullLength + 3);
                    output.WriteInt(fullLength);
                    output.WriteShort(headLength);
                    output.SetWriterIndex(writeIndex);
                }
                else {
                    throw new NotSupportedException("Not support this class:" + msg.GetType());
                }
            }
            catch (Exception e)
            {
                Logger().LogError(e, "Encode request error!");
            }
        }
    }
}
