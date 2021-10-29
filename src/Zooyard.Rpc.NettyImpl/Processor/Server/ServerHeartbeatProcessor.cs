using DotNetty.Transport.Channels;
using System;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Logging;

namespace Zooyard.Rpc.NettyImpl.Processor.Server
{

	/// <summary>
	/// process client heartbeat message request(PING).
	/// <para>
	/// process message type:
	/// <seealso cref="HeartbeatMessage"/>
	/// 
	/// </para>
	/// </summary>
	public class ServerHeartbeatProcessor : IRemotingProcessor
	{

		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ServerHeartbeatProcessor));

		private readonly IRemotingServer _remotingServer;

		public ServerHeartbeatProcessor(IRemotingServer remotingServer)
		{
			_remotingServer = remotingServer;
		}

		public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			try
			{
				await _remotingServer.SendAsyncResponse(rpcMessage, ctx.Channel, HeartbeatMessage.PONG);
			}
			catch (Exception throwable)
			{
				Logger().LogError(throwable, $"send response error: {throwable.Message}");
			}
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogDebug($"received PING from {ctx.Channel.RemoteAddress}");
			}
		}

	}

}