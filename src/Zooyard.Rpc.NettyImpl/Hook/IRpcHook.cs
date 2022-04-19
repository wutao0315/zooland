using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Hook;

public interface IRpcHook
{
    void DoBeforeRequest(string remoteAddr, RpcMessage request);

    void DoAfterResponse(string remoteAddr, RpcMessage request, object response);
}
