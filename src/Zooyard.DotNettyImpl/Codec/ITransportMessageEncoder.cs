using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Codec;

public interface ITransportMessageEncoder
{
    byte[] Encode(TransportMessage message);
}
