using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Zooyard;

namespace Zooyard.Rpc.NettyImpl
{
    internal class HeliosBackwardsCompatabilityLengthFramePrepender : LengthFieldPrepender
    {
        private readonly List<object> _temporaryOutput = new List<object>(2);

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
}
