using DotNetty.Transport.Channels;
using System;
using System.Text;
using Zooyard.Rpc.NettyImpl.Constant;

namespace Zooyard.Rpc.NettyImpl.Protocol
{
    /// <summary>
    /// The type Abstract message.
    /// </summary>
    [Serializable]
	public abstract class AbstractMessage : IMessageTypeAware
    {
        public abstract short TypeCode { get; }

        /// <summary>
		/// The constant UTF8.
		/// </summary>
		protected internal static readonly Encoding UTF8 = Constants.DEFAULT_CHARSET;
        /// <summary>
		/// The Ctx.
		/// </summary>
		protected internal IChannelHandlerContext ctx;
        /// <summary>
		/// Bytes to int int.
		/// </summary>
		/// <param name="bytes">  the bytes </param>
		/// <param name="offset"> the offset </param>
		/// <returns> the int </returns>
		public static int BytesToInt(byte[] bytes, int offset)
        {
            int ret = 0;
            for (int i = 0; i < 4 && i + offset < bytes.Length; i++)
            {
                ret <<= 8;
                ret |= (int)bytes[i + offset] & 0xFF;
            }
            return ret;
        }

        /// <summary>
		/// Int to bytes.
		/// </summary>
		/// <param name="i">      the </param>
		/// <param name="bytes">  the bytes </param>
		/// <param name="offset"> the offset </param>
		public static void IntToBytes(int i, byte[] bytes, int offset)
        {
            bytes[offset] = unchecked((byte)((i >> 24) & 0xFF));
            bytes[offset + 1] = unchecked((byte)((i >> 16) & 0xFF));
            bytes[offset + 2] = unchecked((byte)((i >> 8) & 0xFF));
            bytes[offset + 3] = unchecked((byte)(i & 0xFF));
        }
	}
}