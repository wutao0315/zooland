using DotNetty.Transport.Channels;
using System;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Logging;
using Zooyard.Utils;

namespace Zooyard.Rpc.NettyImpl.Processor.Client
{
	/// <summary>
	/// process TC global commit command.
	/// <para>
	/// process message type:<seealso cref="BranchCommitRequest"/>
	/// </para>
	/// </summary>
	public class RmBranchCommitProcessor : IRemotingProcessor
	{
		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(RmBranchCommitProcessor));

		private ITransactionMessageHandler handler;

		private IRemotingClient remotingClient;

		public RmBranchCommitProcessor(ITransactionMessageHandler handler, IRemotingClient remotingClient)
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
				Logger().LogInformation($"rm client handle branch commit process:{msg}");
			}
			await HandleBranchCommit(rpcMessage, remoteAddress, (BranchCommitRequest) msg);
		}

		private async Task HandleBranchCommit(RpcMessage request, string serverAddress, BranchCommitRequest branchCommitRequest)
		{
			BranchCommitResponse resultMessage = (BranchCommitResponse) (await handler.OnRequest(branchCommitRequest, null));
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogDebug($"branch commit result:{resultMessage}");
			}
			try
			{
				await this.remotingClient.SendAsyncResponse(serverAddress, request, resultMessage);
			}
			catch (Exception throwable)
			{
				Logger().LogError(throwable, $"branch commit error: {throwable.Message}");
			}
		}
	}

}