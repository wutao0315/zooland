using DotNetty.Transport.Channels;
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Processor;

public interface IRemotingProcessor
{
    Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage);
}
