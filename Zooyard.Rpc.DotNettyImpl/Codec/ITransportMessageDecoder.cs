using Zooyard.Rpc.DotNettyImpl.Messages;

namespace Zooyard.Rpc.DotNettyImpl.Codec;

public interface ITransportMessageDecoder
{
    TransportMessage Decode(byte[] data);
}
