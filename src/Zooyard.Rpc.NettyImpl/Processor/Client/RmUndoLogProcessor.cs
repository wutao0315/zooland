using DotNetty.Transport.Channels;
using System;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Logging;

namespace Zooyard.Rpc.NettyImpl.Processor.Client
{
	/// <summary>
	/// process TC undo log delete command.
	/// <para>
	/// process message type:
	/// <seealso cref="UndoLogDeleteRequest"/>
	/// 
	/// </para>
	/// </summary>
	public class RmUndoLogProcessor : IRemotingProcessor
	{
		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(RmUndoLogProcessor));

		private ITransactionMessageHandler handler;

		public RmUndoLogProcessor(ITransactionMessageHandler handler)
		{
			this.handler = handler;
		}

		public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			object msg = rpcMessage.Body;
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogInformation("rm handle undo log process:" + msg);
			}
			await HandleUndoLogDelete((UndoLogDeleteRequest) msg);
		}

		private async Task HandleUndoLogDelete(UndoLogDeleteRequest undoLogDeleteRequest)
		{
			try
			{
				await handler.OnRequest(undoLogDeleteRequest, null);
			}
			catch (Exception ex)
			{
				Logger().LogError(ex, "Failed to delete undo log by undoLogDeleteRequest on" + undoLogDeleteRequest.ResourceId);
			}
		}
	}

}