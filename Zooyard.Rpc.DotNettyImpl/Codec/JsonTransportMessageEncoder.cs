using System.Text;
using Zooyard.Rpc.DotNettyImpl.Messages;

namespace Zooyard.Rpc.DotNettyImpl.Codec;

public sealed class JsonTransportMessageEncoder : ITransportMessageEncoder
{
    public byte[] Encode(TransportMessage message)
    {
        var result = message.ToJsonBytes();
        return result;
    }
}
