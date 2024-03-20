using Zooyard.DotNettyImpl.Messages;

namespace Zooyard.DotNettyImpl.Transport;

public interface ITransportMessageEncoder
{
    byte[] Encode(TransportMessage message);
}
