using DotNetty.Transport.Channels;
using System.Collections.Concurrent;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Processor.Client;

/// <summary>
/// process TC response message.
/// <para>
/// process message type:
/// RM:
/// 1) <seealso cref="MergeResultMessage"/>
/// 2) <seealso cref="RegisterRMResponse"/>
/// 3) <seealso cref="BranchRegisterResponse"/>
/// 4) <seealso cref="BranchReportResponse"/>
/// 5) <seealso cref="GlobalLockQueryResponse"/>
/// TM:
/// 1) <seealso cref="MergeResultMessage"/>
/// 2) <seealso cref="RegisterTMResponse"/>
/// 3) <seealso cref="GlobalBeginResponse"/>
/// 4) <seealso cref="GlobalCommitResponse"/>
/// 5) <seealso cref="GlobalReportResponse"/>
/// 6) <seealso cref="GlobalRollbackResponse"/>
/// 
/// @author zhangchenghui.dev@gmail.com
/// @since 1.3.0
/// </para>
/// </summary>
public class ClientOnResponseProcessor : IRemotingProcessor
{

	private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ClientOnResponseProcessor));
	/// <summary>
	/// The Merge msg map from io.seata.core.rpc.netty.AbstractNettyRemotingClient#mergeMsgMap.
	/// </summary>
	private IDictionary<int, IMergeMessage> mergeMsgMap;

	/// <summary>
	/// The Futures from io.seata.core.rpc.netty.AbstractNettyRemoting#futures
	/// </summary>
	private ConcurrentDictionary<int, MessageFuture> futures;

	/// <summary>
	/// To handle the received RPC message on upper level.
	/// </summary>
	private ITransactionMessageHandler transactionMessageHandler;

	public ClientOnResponseProcessor(IDictionary<int, IMergeMessage> mergeMsgMap, 
		ConcurrentDictionary<int, MessageFuture> futures,
		ITransactionMessageHandler transactionMessageHandler)
	{
		this.mergeMsgMap = mergeMsgMap;
		this.futures = futures;
		this.transactionMessageHandler = transactionMessageHandler;
	}

	public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
	{
		if (rpcMessage.Body is MergeResultMessage results)
		{
			if (mergeMsgMap != null 
				&& mergeMsgMap.TryGetValue(rpcMessage.Id, out IMergeMessage mergeMessageData)) 
			{
				var mergeMessage = (MergedWarpMessage)mergeMessageData;
				for (int i = 0; i < mergeMessage.msgs.Count; i++)
				{
					int msgId = mergeMessage.msgIds[i];
					if (!futures.TryRemove(msgId, out MessageFuture future) || future == null)
					{
						if (Logger().IsEnabled(level: LogLevel.Debug))
						{
							Logger().LogInformation($"msg: {msgId} is not found in futures.");
						}
					}
					else
					{
						future.ResultMessage = results.Msgs[i];
					}
				}
			}
		}
		else
		{
			if (futures.TryRemove(rpcMessage.Id, out MessageFuture messageFuture) && messageFuture != null)
			{
				messageFuture.ResultMessage = rpcMessage.Body;
			}
			else
			{
				if (rpcMessage.Body is AbstractResultMessage result)
				{
					if (transactionMessageHandler != null)
					{
						await transactionMessageHandler.OnResponse(result, null);
					}
				}
			}
		}
	}
}
