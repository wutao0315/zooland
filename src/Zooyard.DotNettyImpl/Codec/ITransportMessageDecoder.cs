using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Codec;

public interface ITransportMessageDecoder
{
    TransportMessage Decode(byte[] data);
}
