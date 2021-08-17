using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Logging;
using Zooyard.Utils;
using Zooyard.Rpc.NettyImpl.Support;

namespace Zooyard.Rpc.NettyImpl.Processor.Server
{
	/// <summary>
	/// handle RM/TM response message.
	/// <para>
	/// process message type:
	/// RM:
	/// 1) <seealso cref="BranchCommitResponse"/>
	/// 2) <seealso cref="BranchRollbackResponse"/>
	/// 
	/// </para>
	/// </summary>
	public class ServerOnResponseProcessor : IRemotingProcessor
	{

		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ServerOnRequestProcessor));
		/// <summary>
		/// To handle the received RPC message on upper level.
		/// </summary>
		private ITransactionMessageHandler transactionMessageHandler;

		/// <summary>
		/// The Futures from io.seata.core.rpc.netty.AbstractNettyRemoting#futures
		/// </summary>
		private ConcurrentDictionary<int, MessageFuture> futures;

		public ServerOnResponseProcessor(ITransactionMessageHandler transactionMessageHandler, ConcurrentDictionary<int, MessageFuture> futures)
		{
			this.transactionMessageHandler = transactionMessageHandler;
			this.futures = futures;
		}

		public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			if (futures.TryRemove(rpcMessage.Id, out MessageFuture messageFuture) && messageFuture != null)
			{
				messageFuture.ResultMessage = rpcMessage.Body;
			}
			else
			{
				if (ChannelManager.IsRegistered(ctx.Channel))
				{
					await OnResponseMessage(ctx, rpcMessage);
				}
				else
				{
					try
					{
						if (Logger().IsEnabled(LogLevel.Debug))
						{
							Logger().LogInformation("closeChannelHandlerContext channel:" + ctx.Channel);
						}
						await ctx.DisconnectAsync();
						await ctx.CloseAsync();
					}
					catch (Exception exx)
					{
						Logger().LogError(exx, exx.Message);
					}
					if (Logger().IsEnabled(LogLevel.Debug))
					{
						Logger().LogInformation($"close a unhandled connection! [{ctx.Channel}]");
					}
				}
			}
		}

		private async Task OnResponseMessage(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogDebug($"server received:{rpcMessage.Body},clientIp:{NetUtil.ToIpAddress(ctx.Channel.RemoteAddress)},vgroup:{ChannelManager.GetContextFromIdentified(ctx.Channel).TransactionServiceGroup}");
			}
			else
			{
				try
				{
					BatchLogHandler.INSTANCE.LogQueue.Add($"{rpcMessage.Body},clientIp:{NetUtil.ToIpAddress(ctx.Channel.RemoteAddress)},vgroup:{ChannelManager.GetContextFromIdentified(ctx.Channel).TransactionServiceGroup}");
				}
				catch (Exception e)
				{
					Logger().LogError(e, $"put message to logQueue error: {e.Message}");
				}
			}
			if (rpcMessage.Body is AbstractResultMessage abstractResultMessage)
			{
				RpcContext rpcContext = ChannelManager.GetContextFromIdentified(ctx.Channel);
				await transactionMessageHandler.OnResponse(abstractResultMessage, rpcContext);
			}
		}
	}

}