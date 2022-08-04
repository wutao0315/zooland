using DotNetty.Transport.Channels;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.NettyImpl.Support;
using Zooyard.Utils;

namespace Zooyard.Rpc.NettyImpl.Processor.Server;

/// <summary>
/// process RM/TM client request message.
/// <para>
/// message type:
/// RM:
/// 1) <seealso cref="MergedWarpMessage"/>
/// 2) <seealso cref="BranchRegisterRequest"/>
/// 3) <seealso cref="BranchReportRequest"/>
/// 4) <seealso cref="GlobalLockQueryRequest"/>
/// TM:
/// 1) <seealso cref="MergedWarpMessage"/>
/// 2) <seealso cref="GlobalBeginRequest"/>
/// 3) <seealso cref="GlobalCommitRequest"/>
/// 4) <seealso cref="GlobalReportRequest"/>
/// 5) <seealso cref="GlobalRollbackRequest"/>
/// 6) <seealso cref="GlobalStatusRequest"/>
/// 
/// </para>
/// </summary>
public class ServerOnRequestProcessor : IRemotingProcessor
{
	private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ServerOnRequestProcessor));

	private readonly IRemotingServer _remotingServer;

	private readonly ITransactionMessageHandler _transactionMessageHandler;

	public ServerOnRequestProcessor(IRemotingServer remotingServer,
		ITransactionMessageHandler transactionMessageHandler)
	{
		_remotingServer = remotingServer;
		_transactionMessageHandler = transactionMessageHandler;
	}

	public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
	{
		if (ChannelManager.IsRegistered(ctx.Channel))
		{
			await OnRequestMessage(ctx, rpcMessage);
		}
		else
		{
			try
			{
				if (Logger().IsEnabled(LogLevel.Debug))
				{
					Logger().LogInformation($"closeChannelHandlerContext channel:{ctx.Channel}");
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

	private async Task OnRequestMessage(IChannelHandlerContext ctx, RpcMessage rpcMessage)
	{
		object message = rpcMessage.Body;
		RpcContext rpcContext = ChannelManager.GetContextFromIdentified(ctx.Channel);
		if (Logger().IsEnabled(LogLevel.Debug))
		{
			Logger().LogDebug($"server received:{message},clientIp:{NetUtil.ToIpAddress(ctx.Channel.RemoteAddress)},vgroup:{rpcContext.TransactionServiceGroup}");
		}
		else
		{
			try
			{
				BatchLogHandler.INSTANCE.LogQueue.Add($"{message},clientIp:{NetUtil.ToIpAddress(ctx.Channel.RemoteAddress)},vgroup:{rpcContext.TransactionServiceGroup}");
			}
			catch (Exception e)
			{
				Logger().LogError(e, $"put message to logQueue error: {e.Message}");
			}
		}
		if (!(message is AbstractMessage))
		{
			return;
		}
		if (message is MergedWarpMessage mergedWarpMessage)
		{
			AbstractResultMessage[] results = new AbstractResultMessage[mergedWarpMessage.msgs.Count];
			for (int i = 0; i < results.Length; i++)
			{
				AbstractMessage subMessage = mergedWarpMessage.msgs[i];
				results[i] = await _transactionMessageHandler.OnRequest(subMessage, rpcContext);
			}
                MergeResultMessage resultMessage = new ()
                {
                    Msgs = results
                };
                await _remotingServer.SendAsyncResponse(rpcMessage, ctx.Channel, resultMessage);
		}
		else
		{
			// the single send request message
			var msg = (AbstractMessage) message;
			AbstractResultMessage result = await _transactionMessageHandler.OnRequest(msg, rpcContext);
			await _remotingServer.SendAsyncResponse(rpcMessage, ctx.Channel, result);
		}
	}
}
