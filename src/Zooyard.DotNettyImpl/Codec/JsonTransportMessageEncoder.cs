using System.Text;
using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Codec;

public sealed class JsonTransportMessageEncoder : ITransportMessageEncoder
{
    public byte[] Encode(TransportMessage message)
    {
        var result = message.ToJsonBytes();
        return result;
    }
}
