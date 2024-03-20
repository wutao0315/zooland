namespace Zooyard.Realtime.Protocol;

public record CancelInvocationMessage(string invocationId) : RpcInvocationMessage(invocationId)
{
}
