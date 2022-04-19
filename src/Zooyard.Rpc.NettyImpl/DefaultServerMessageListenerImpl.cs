using DotNetty.Transport.Channels;
using System.Collections.Concurrent;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.NettyImpl.Support;
using Zooyard.Utils;

namespace Zooyard.Rpc.NettyImpl;

/// <summary>
/// The type Default server message listener.
/// 
/// </summary>
public class DefaultServerMessageListenerImpl//: IServerMessageListener
{
	private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(DefaultServerMessageListenerImpl));

	private static BlockingCollection<string> logQueue = new();
	private IRemotingServer remotingServer;
	private readonly ITransactionMessageHandler _transactionMessageHandler;
        private const int MAX_LOG_SEND_THREAD = 1;
        private const int MAX_LOG_TAKE_SIZE = 1024;
        //private const long KEEP_ALIVE_TIME = 0L;
        //private const string THREAD_PREFIX = "batchLoggerPrint";
        private const long BUSY_SLEEP_MILLS = 5L;

        /// <summary>
        /// Instantiates a new Default server message listener.
        /// </summary>
        /// <param name="transactionMessageHandler"> the transaction message handler </param>
        public DefaultServerMessageListenerImpl(ITransactionMessageHandler transactionMessageHandler)
	{
            _transactionMessageHandler = transactionMessageHandler;
	}

	public virtual async Task OnTrxMessage(RpcMessage request, IChannelHandlerContext ctx)
	{
            object message = request.Body;
            RpcContext rpcContext = ChannelManager.GetContextFromIdentified(ctx.Channel);
		if (Logger().IsEnabled(LogLevel.Debug))
		{
			Logger().LogDebug($"server received:{message},clientIp:{NetUtil.ToIpAddress(ctx.Channel.RemoteAddress)},vgroup:{rpcContext.TransactionServiceGroup}");
		}
		else 
		{
			try
			{
				logQueue.Add($"{message},clientIp:{NetUtil.ToIpAddress(ctx.Channel.RemoteAddress)},vgroup:{rpcContext.TransactionServiceGroup}");
			}
			catch (Exception e)
			{
				Logger().LogError(e, $"put message to logQueue error: {e.Message}");
			}
		}

            if (message is not AbstractMessage msg)
		{
			return;
		}
		if (message is MergedWarpMessage mergedMsg)
		{
			var results = new AbstractResultMessage[mergedMsg.msgs.Count];
			for (int i = 0; i < results.Length; i++)
			{
				AbstractMessage subMessage = mergedMsg.msgs[i];
				results[i] = await _transactionMessageHandler.OnRequest(subMessage, rpcContext);
			}
                var resultMessage = new MergeResultMessage
                {
                    Msgs = results
                };
                await ServerMessageSender.SendAsyncResponse(request, ctx.Channel, resultMessage);
		}
		else if (message is AbstractResultMessage message1)
		{
			await _transactionMessageHandler.OnResponse(message1, rpcContext);
		}
		else {
			AbstractResultMessage result = await _transactionMessageHandler.OnRequest(msg, rpcContext);
			await ServerMessageSender.SendAsyncResponse(request, ctx.Channel, result);
		}
	}

	public virtual async Task OnCheckMessage(RpcMessage request, IChannelHandlerContext ctx)
	{
		try
		{
			await ServerMessageSender.SendAsyncResponse(request, ctx.Channel, HeartbeatMessage.PONG);
		}
		catch (Exception throwable)
		{
			Logger().LogError(throwable, $"send response error:{throwable.Message}");
		}
		if (Logger().IsEnabled(LogLevel.Debug)) 
		{
			Logger().LogDebug($"received PING from { ctx.Channel.RemoteAddress}");
		}
            
        }

	/// <summary>
	/// Init.
	/// </summary>
	public virtual void Init()
	{
		//var mergeSendExecutorService = new ThreadPoolExecutor(MAX_LOG_SEND_THREAD, 
		//	MAX_LOG_SEND_THREAD, 
		//	TimeSpan.FromMilliseconds(KEEP_ALIVE_TIME) ,
		//	new BlockingCollection<object>(),
		//	new NamedThreadFactory(THREAD_PREFIX, MAX_LOG_SEND_THREAD, true));
		var mergeSendExecutorService =new  MultithreadEventLoopGroup(MAX_LOG_SEND_THREAD);

		mergeSendExecutorService.SubmitAsync(() => 
		{
			var logList = new List<string>();
			while (true)
			{
				try
				{
					var takeCount = 0;
					while (logQueue.TryTake(out string item))
					{
						logList.Add(item);
						takeCount++;
						if (takeCount >= MAX_LOG_TAKE_SIZE)
						{
							break;
						}
					}

					if (Logger().IsEnabled(LogLevel.Information))
					{
						foreach (string str in logList)
						{
							Logger().LogInformation(str);
						}
					}

					logList.Clear();
					Thread.Sleep(TimeSpan.FromMilliseconds(BUSY_SLEEP_MILLS));
				}
				catch (Exception exx)
				{
					Logger().LogError(exx, $"batch log busy sleep error:{exx.Message}");
				}
			}
			return true;
		});
	}

	/// <summary>
	/// Gets server message sender.
	/// </summary>
	/// <returns> the server message sender </returns>
	public virtual IRemotingServer ServerMessageSender
	{
		get
		{
			if (remotingServer == null)
			{
				throw new ArgumentException("serverMessageSender must not be null");
			}
			return remotingServer;
		}
		set
		{
			this.remotingServer = value;
		}
	}
}
