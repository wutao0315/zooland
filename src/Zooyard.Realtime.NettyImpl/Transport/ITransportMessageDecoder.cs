using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Transport;

public interface ITransportMessageDecoder
{
    TransportMessage Decode(byte[] data);
}
