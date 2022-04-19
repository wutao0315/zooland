using DotNetty.Transport.Channels;
using Zooyard.Atomic;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Processor.Server;
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Support;

/// <summary>
/// The netty remoting server.
/// </summary>
public class NettyRemotingServer : AbstractNettyRemotingServer
{

	private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyRemotingServer));

	//private ITransactionMessageHandler transactionMessageHandler;

	private readonly AtomicBoolean initialized = new (false);

	public override async Task Init()
	{
		// registry processor
		await RegisterProcessor();
		if (initialized.CompareAndSet(false, true))
		{
			await base.Init();
		}
	}

	/// <summary>
	/// Instantiates a new Rpc remoting server.
	/// </summary>
	/// <param name="messageExecutor">   the message executor </param>
	public NettyRemotingServer(MultithreadEventLoopGroup messageExecutor) // ThreadPoolExecutor messageExecutor)
		: base(messageExecutor, new NettyServerConfig())
	{
	}

	/// <summary>
	/// Sets transactionMessageHandler.
	/// </summary>
	/// <param name="transactionMessageHandler"> the transactionMessageHandler </param>
	public virtual ITransactionMessageHandler Handler { get; set; }


	public override async Task DestroyChannel(string serverAddress, IChannel channel)
	{
		if (Logger().IsEnabled(LogLevel.Information))
		{
			Logger().LogInformation($"will destroy channel:{channel},address:{serverAddress}");
		}
		await channel.DisconnectAsync();
		await channel.CloseAsync();
	}

	private async Task RegisterProcessor()
	{
		// 1. registry on request message processor
		var onRequestProcessor = new ServerOnRequestProcessor(this, Handler);
		await base.RegisterProcessor(MessageType.TYPE_BRANCH_REGISTER, onRequestProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_BRANCH_STATUS_REPORT, onRequestProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_GLOBAL_BEGIN, onRequestProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_GLOBAL_COMMIT, onRequestProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_GLOBAL_LOCK_QUERY, onRequestProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_GLOBAL_REPORT, onRequestProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_GLOBAL_ROLLBACK, onRequestProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_GLOBAL_STATUS, onRequestProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_SEATA_MERGE, onRequestProcessor, messageExecutor);
		// 2. registry on response message processor
		var onResponseProcessor = new ServerOnResponseProcessor(Handler, Futures);
		await base.RegisterProcessor(MessageType.TYPE_BRANCH_COMMIT_RESULT, onResponseProcessor, messageExecutor);
		await base.RegisterProcessor(MessageType.TYPE_BRANCH_ROLLBACK_RESULT, onResponseProcessor, messageExecutor);
		// 3. registry heartbeat message processor
		var heartbeatMessageProcessor = new ServerHeartbeatProcessor(this);
		await base.RegisterProcessor(MessageType.TYPE_HEARTBEAT_MSG, heartbeatMessageProcessor, null);
	}
}
