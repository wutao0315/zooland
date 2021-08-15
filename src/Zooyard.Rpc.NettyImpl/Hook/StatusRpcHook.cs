using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Hook
{
    public class StatusRpcHook : IRpcHook
	{

		public virtual void DoBeforeRequest(string remoteAddr, RpcMessage request)
		{
			RpcStatus.BeginCount(remoteAddr);
		}

		public virtual void DoAfterResponse(string remoteAddr, RpcMessage request, object response)
		{
			RpcStatus.EndCount(remoteAddr);
		}
	}
}
