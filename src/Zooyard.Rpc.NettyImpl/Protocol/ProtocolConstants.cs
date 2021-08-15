using System;

namespace Zooyard.Rpc.NettyImpl.Protocol
{
    public class ProtocolConstants
    {
        /// <summary>
        /// Magic code
        /// </summary>
        public static readonly byte[] MAGIC_CODE_BYTES = new [] { (byte)0xda, (byte)0xda };

        /// <summary>
        /// Protocol version
        /// </summary>
        public const byte VERSION = 1;

        /// <summary>
        /// Max frame length
        /// </summary>
        public const int MAX_FRAME_LENGTH = 8 * 1024 * 1024;

        /// <summary>
        /// HEAD_LENGTH of protocol v1
        /// </summary>
        public const int V1_HEAD_LENGTH = 16;
        /// <summary>
        /// Message type: Request
        /// </summary>
        public const byte MSGTYPE_RESQUEST_SYNC = 0;
        /// <summary>
        /// Message type: Request
        /// </summary>
        public const byte MSGTYPE_RESQUEST = 0;
        /// <summary>
        /// Message type: Response
        /// </summary>
        public const byte MSGTYPE_RESPONSE = 1;
        /// <summary>
        /// Message type: Request which no need response
        /// </summary>
        public const byte MSGTYPE_RESQUEST_ONEWAY = 2;
        /// <summary>
        /// Message type: Heartbeat Request
        /// </summary>
        public const byte MSGTYPE_HEARTBEAT_REQUEST = 3;
        /// <summary>
        /// Message type: Heartbeat Response
        /// </summary>
        public const byte MSGTYPE_HEARTBEAT_RESPONSE = 4;

        /// <summary>
        /// Configured codec by user, default is ZOOTA
        /// </summary>
        public readonly static byte CONFIGURED_CODEC = (byte)Enum.Parse<SerializerType>(ConfigurationFactory.Instance.GetValue(Constant.ConfigurationKeys.SERIALIZE_FOR_RPC, SerializerType.ZOOTA.ToString()), true);

        /// <summary>
        ///  Configured compressor by user, default is NONE
        /// </summary>
        public readonly static byte CONFIGURED_COMPRESSOR = (byte)Enum.Parse<CompressorType>(ConfigurationFactory.Instance.GetValue(Constant.ConfigurationKeys.COMPRESSOR_FOR_RPC, CompressorType.NONE.ToString()), true);
    }
}
