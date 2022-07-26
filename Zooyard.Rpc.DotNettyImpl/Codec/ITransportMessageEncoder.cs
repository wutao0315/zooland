using Zooyard.Rpc.DotNettyImpl.Messages;

namespace Zooyard.Rpc.DotNettyImpl.Codec;

public interface ITransportMessageEncoder
{
    byte[] Encode(TransportMessage message);
}
