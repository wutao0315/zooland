using DotNetty.Transport.Channels;
using System;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Logging;
using Zooyard.Utils;

namespace Zooyard.Rpc.NettyImpl.Processor.Client
{
	/// <summary>
	/// process TC do global rollback command.
	/// <para>
	/// process message type: <seealso cref="BranchRollbackRequest"/>
	/// </para>
	/// </summary>
	public class RmBranchRollbackProcessor : IRemotingProcessor
	{

		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(RmBranchRollbackProcessor));

		private ITransactionMessageHandler handler;

		private IRemotingClient remotingClient;

		public RmBranchRollbackProcessor(ITransactionMessageHandler handler, IRemotingClient remotingClient)
		{
			this.handler = handler;
			this.remotingClient = remotingClient;
		}

		public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			string remoteAddress = NetUtil.ToStringAddress(ctx.Channel.RemoteAddress);
			object msg = rpcMessage.Body;
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogInformation("rm handle branch rollback process:" + msg);
			}
			await HandleBranchRollback(rpcMessage, remoteAddress, (BranchRollbackRequest) msg);
		}

        private async Task HandleBranchRollback(RpcMessage request, string serverAddress, BranchRollbackRequest branchRollbackRequest)
		{
			BranchRollbackResponse resultMessage = (BranchRollbackResponse) await handler.OnRequest(branchRollbackRequest, null);
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogDebug($"branch rollback result:{resultMessage}");
			}
			try
			{
				await this.remotingClient.SendAsyncResponse(serverAddress, request, resultMessage);
			}
			catch (Exception throwable)
			{
				Logger().LogError(throwable, $"send response error: {throwable.Message}");
			}
		}
	}

}