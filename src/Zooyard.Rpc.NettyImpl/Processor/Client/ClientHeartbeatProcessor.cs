using DotNetty.Transport.Channels;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Processor.Client;

/// <summary>
/// process TC heartbeat message request(PONG)
/// <para>
/// process message type:
/// <seealso cref="HeartbeatMessage"/>
/// </para>
/// </summary>
public class ClientHeartbeatProcessor : IRemotingProcessor
{
	private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ClientHeartbeatProcessor));

	public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
	{
		if (rpcMessage.Body == HeartbeatMessage.PONG)
		{
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogDebug($"received PONG from {ctx.Channel.RemoteAddress}");
			}
		}
		await Task.CompletedTask;
	}
}
