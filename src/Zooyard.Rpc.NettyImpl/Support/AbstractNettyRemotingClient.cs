using DotNetty.Common.Concurrency;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Zooyard.Exceptions;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Processor;
using Zooyard.Rpc.NettyImpl.Protocol;


namespace Zooyard.Rpc.NettyImpl.Support;

/// <summary>
/// The type Rpc remoting client.
/// 
/// </summary>
public abstract class AbstractNettyRemotingClient : AbstractNettyRemoting, IRemotingClient
{
    public abstract Task OnRegisterMsgFail(string serverAddress, IChannel channel, object response, AbstractMessage requestMessage);
    public abstract Task OnRegisterMsgSuccess(string serverAddress, IChannel channel, object response, AbstractMessage requestMessage);


    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(AbstractNettyRemotingClient));
    private const string MSG_ID_PREFIX = "msgId:";
		private const string FUTURES_PREFIX = "futures:";
		private const string SINGLE_LOG_POSTFIX = ";";
		private const int MAX_MERGE_SEND_MILLS = 1;
		//private const string THREAD_PREFIX_SPLIT_CHAR = "_";


    private const int MAX_MERGE_SEND_THREAD = 1;
    //private static readonly long KEEP_ALIVE_TIME = int.MaxValue;
    private static readonly int SCHEDULE_DELAY_MILLS = 60 * 1000;
    private static readonly int SCHEDULE_INTERVAL_MILLS = 10 * 1000;
    //private const string MERGE_THREAD_PREFIX = "rpcMergeMessageSend";
    protected internal readonly object mergeLock = new ();
    protected internal readonly object @lock = new ();
    

    /// <summary>
		/// When sending message type is <seealso cref="MergeMessage"/>, will be stored to mergeMsgMap.
		/// </summary>
		protected internal readonly ConcurrentDictionary<int, IMergeMessage> mergeMsgMap = new ();

    /// <summary>
		/// When batch sending is enabled, the message will be stored to basketMap
		/// Send via asynchronous thread <seealso cref="MergedSendRunnable"/>
		/// <seealso cref="NettyClientConfig#isEnableClientBatchSendRequest"/>
		/// </summary>
		protected internal readonly ConcurrentDictionary<string, BlockingCollection<RpcMessage>> basketMap = new (); //serverAddress


    private readonly NettyClientBootstrap clientBootstrap;
    private readonly NettyClientChannelManager clientChannelManager;
    //private ThreadPoolExecutor mergeSendExecutorService;
    private MultithreadEventLoopGroup mergeSendExecutorService;
    //private ITransactionMessageHandler transactionMessageHandler;

    private Task taskExecutor;
    public override async Task Init()
    {
        //scheduleAtFixedRate
        taskExecutor = new Task(async () =>
        {
            Thread.Sleep(SCHEDULE_DELAY_MILLS);
            while (true)
            {
                await clientChannelManager.Reconnect(TransactionServiceGroup);
                Thread.Sleep(SCHEDULE_INTERVAL_MILLS);
            }
        }, TaskCreationOptions.LongRunning);
        taskExecutor.Start();

        if (NettyClientConfig.EnableClientBatchSendRequest)
        {
            //mergeSendExecutorService = new ThreadPoolExecutor(MAX_MERGE_SEND_THREAD, MAX_MERGE_SEND_THREAD,
            //    TimeSpan.FromMilliseconds(KEEP_ALIVE_TIME),
            //    new BlockingCollection<object>(),
            //    new NamedThreadFactory(ThreadPrefix, MAX_MERGE_SEND_THREAD));
            //mergeSendExecutorService.Submit(()=> new MergedSendRunnable(this).Run());
            mergeSendExecutorService = new MultithreadEventLoopGroup(MAX_MERGE_SEND_THREAD);
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
            mergeSendExecutorService.SubmitAsync(async() => await new MergedSendRunnable(this).Run());
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
        }
        await base.Init();
        await clientBootstrap.Start();
    }

    public AbstractNettyRemotingClient(
        NettyClientConfig nettyClientConfig,
        IEventExecutorGroup eventExecutorGroup,
        //ThreadPoolExecutor messageExecutor,
        MultithreadEventLoopGroup messageExecutor
        )
        : base(messageExecutor)
    {
        //this.TransactionServiceGroup = zooTaOption.CurrentValue.TransactionServiceGroup;

        clientBootstrap = new NettyClientBootstrap(nettyClientConfig, eventExecutorGroup);//, nettyTransportClientConfigOptoin);
        clientBootstrap.SetChannelHandlers(new ClientHandler(this));
        clientChannelManager = new NettyClientChannelManager(new NettyPoolableFactory(this, clientBootstrap), PoolKeyFunction, nettyClientConfig);
    }
    public virtual async Task<object> SendSyncRequest(object msg)
    {
        string serverAddress = LoadBalance(TransactionServiceGroup, msg);
        int timeoutMillis = NettyClientConfig.RpcRequestTimeout;
        RpcMessage rpcMessage = BuildRequestMessage(msg, ProtocolConstants.MSGTYPE_RESQUEST_SYNC);

        // send batch message
        // put message into basketMap, @see MergedSendRunnable
        if (NettyClientConfig.EnableClientBatchSendRequest)
        {
            // send batch message is sync request, needs to create messageFuture and put it in futures.
            MessageFuture messageFuture = new (rpcMessage, TimeSpan.FromMilliseconds(timeoutMillis));
            _futures[rpcMessage.Id] = messageFuture;

            // put message into basketMap
            BlockingCollection<RpcMessage> basket = basketMap.GetOrAdd(serverAddress, key => new BlockingCollection<RpcMessage>());

            
            if (!basket.TryAdd(rpcMessage))
            {
                Logger().LogError($"put message into basketMap offer failed, serverAddress:{serverAddress},rpcMessage:{rpcMessage}");
                return null;
            }
            if (Logger().IsEnabled(LogLevel.Debug))
            {
                Logger().LogDebug($"offer message: {rpcMessage.Body}");
            }
            if (!isSending)
            {
                lock (mergeLock)
                {
                    Monitor.PulseAll(mergeLock);
                }
            }

            try
            {
                return messageFuture.Get();
            }
            catch (Exception exx)
            {
                Logger().LogError($"wait response error:{exx.Message},ip:{serverAddress},request:{rpcMessage.Body}");
                if (exx is TimeoutException exception)
                {
                    throw exception;
                }
                else
                {
                    throw;
                }
            }
        }
        else
        {
            IChannel channel = await clientChannelManager.AcquireChannel(serverAddress);
            return await base.SendSync(channel, rpcMessage, timeoutMillis);
        }

    }
    public virtual async Task<object> SendSyncRequest(IChannel channel, object msg)
    {
        if (channel == null)
        {
            Logger().LogWarning("sendSyncRequest nothing, caused by null channel.");
            return null;
        }
        RpcMessage rpcMessage = BuildRequestMessage(msg, ProtocolConstants.MSGTYPE_RESQUEST_SYNC);
        return await base.SendSync(channel, rpcMessage, NettyClientConfig.RpcRequestTimeout);
    }

    public virtual async Task SendAsyncRequest(IChannel channel, object msg)
    {
        if (channel == null)
        {
            Logger().LogWarning("sendAsyncRequest nothing, caused by null channel.");
            return;
        }
        RpcMessage rpcMessage = BuildRequestMessage(msg, msg is HeartbeatMessage ? ProtocolConstants.MSGTYPE_HEARTBEAT_REQUEST : ProtocolConstants.MSGTYPE_RESQUEST_ONEWAY);
        if (rpcMessage.Body is IMergeMessage bodyMsg)
        {
            mergeMsgMap[rpcMessage.Id] = bodyMsg;
        }
        await base.SendAsync(channel, rpcMessage);
    }

    public virtual async Task SendAsyncResponse(string serverAddress, RpcMessage rpcMessage, object msg)
    {
        RpcMessage rpcMsg = BuildResponseMessage(rpcMessage, msg, ProtocolConstants.MSGTYPE_RESPONSE);
        IChannel channel = await clientChannelManager.AcquireChannel(serverAddress);
        await base.SendAsync(channel, rpcMsg);
    }

    public virtual async Task RegisterProcessor(int requestCode, IRemotingProcessor processor, IExecutorService executor)
    {
        Pair<IRemotingProcessor, IExecutorService> pair = new (processor, executor);
        this._processorTable[requestCode] = pair;
        await Task.CompletedTask;
    }

    public override async Task DestroyChannel(string serverAddress, IChannel channel)
    {
        await clientChannelManager.DestroyChannel(serverAddress, channel);
    }

    public override async ValueTask DisposeAsync()
    {
        await clientBootstrap?.Shutdown();
        await mergeSendExecutorService?.ShutdownGracefullyAsync();
        //await mergeSendExecutorService.DisposeAsync();
        taskExecutor?.Dispose();
        await base.DisposeAsync();
    }

    public virtual ITransactionMessageHandler TransactionMessageHandler { get; set; }
    public virtual NettyClientChannelManager ClientChannelManager => clientChannelManager;


    protected internal virtual string LoadBalance(string transactionServiceGroup, object msg)
    {
        IPEndPoint address = null;
        try
        {
            //IList<IPEndPoint> inetSocketAddressList = RegistryFactory.Instance.lookup(transactionServiceGroup);
            //address = this.doSelect(inetSocketAddressList, msg);
            address = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8091);
        }
        catch (Exception ex)
        {
            Logger().LogError(ex, ex.Message);
        }
        if (address == null)
        {
            throw new FrameworkException("NoAvailableService");
        }
        return Utils.NetUtil.ToStringAddress(address);
    }

    //protected internal virtual IPEndPoint DoSelect(IList<IPEndPoint> list, object msg)
    //{
    //    if (list?.Count > 0)
    //    {
    //        if (list.Count > 1)
    //        {
    //            return LoadBalanceFactory.Instance.Select(list, GetXid(msg));
    //        }
    //        else
    //        {
    //            return list[0];
    //        }
    //    }
    //    return null;
    //}

    //protected internal virtual string GetXid(object msg)
    //{
    //    string xid = "";
    //    if (msg is AbstractGlobalEndRequest abstractGlobalEndRequest)
    //    {
    //        xid = abstractGlobalEndRequest.Xid;
    //    }
    //    else if (msg is GlobalBeginRequest globalBeginRequest)
    //    {
    //        xid = globalBeginRequest.TransactionName;
    //    }
    //    else if (msg is BranchRegisterRequest branchRegisterRequest)
    //    {
    //        xid = branchRegisterRequest.Xid;
    //    }
    //    else if (msg is BranchReportRequest branchReportRequest)
    //    {
    //        xid = branchReportRequest.Xid;
    //    }
    //    else
    //    {
    //        try
    //        {
    //            var field = msg.GetType().GetField("xid");
    //            xid = field.GetValue(msg).ToString();
    //        }
    //        catch (Exception)
    //        {
    //        }
    //    }
    //    return string.IsNullOrWhiteSpace(xid) ? new Random().NextLong().ToString() : xid;
    //}


    /// <summary>
    /// Get pool key function.
    /// </summary>
    /// <returns> lambda function </returns>
    protected internal abstract Func<string, NettyPoolKey> PoolKeyFunction { get; }
    /// <summary>
		/// Get transaction service group.
		/// </summary>
		/// <returns> transaction service group </returns>
		protected internal abstract string TransactionServiceGroup { get; }

    /// <summary>
    /// The type Merged send runnable.
    /// </summary>
    private class MergedSendRunnable
    {
        private readonly AbstractNettyRemotingClient outerInstance;

        public MergedSendRunnable(AbstractNettyRemotingClient outerInstance)
        {
            this.outerInstance = outerInstance;
        }


        public async Task Run()
        {
            while (true)
            {
                lock (outerInstance.mergeLock)
                {
                    try
                    {
                        Monitor.Wait(outerInstance.mergeLock, TimeSpan.FromMilliseconds(MAX_MERGE_SEND_MILLS));
                    }
                    catch (Exception)
                    {
                    }
                }
                outerInstance.isSending = true;

                // send batch message is sync request, but there is no need to get the return value.
                // Since the messageFuture has been created before the message is placed in basketMap,
                // the return value will be obtained in ClientOnResponseProcessor.
                // fast fail
                foreach (var item in outerInstance.basketMap)
                {
                    var address = item.Key;

                    if (item.Value.Count <= 0)
                    {
                        return;
                    }
                    var mergeMessage = new MergedWarpMessage();
                    while (item.Value.Count > 0)
                    {
                        RpcMessage msg = item.Value.Take();
                        mergeMessage.msgs.Add((AbstractMessage)msg.Body);
                        mergeMessage.msgIds.Add(msg.Id);
                    }
                    if (mergeMessage.msgIds.Count > 1)
                    {
                        PrintMergeMessageLog(mergeMessage);
                    }
                    IChannel sendChannel = null;
                    try
                    {
                        sendChannel = await outerInstance.clientChannelManager.AcquireChannel(address);
                        await outerInstance.SendAsyncRequest(sendChannel, mergeMessage);
                    }
                    catch (FrameworkException e)
                    {
                        if (e.Errcode == FrameworkErrorCode.ChannelIsNotWritable && sendChannel != null)
                        {
                            await outerInstance.DestroyChannel(address, sendChannel);
                        }
                        foreach (int msgId in mergeMessage.msgIds)
                        {
                            if (outerInstance._futures.TryRemove(msgId, out MessageFuture messageFuture) && messageFuture != null)
                            {
                                messageFuture.ResultMessage = null;
                            }
                        }
                        Logger().LogError(e, $"client merge call failed: {e.Message}");
                    }
                }

                outerInstance.isSending = false;
            }
        }

        internal virtual void PrintMergeMessageLog(MergedWarpMessage mergeMessage)
        {
            if (Logger().IsEnabled(LogLevel.Debug))
            {
                Logger().LogDebug($"merge msg size:{mergeMessage.msgIds.Count}");
                foreach (AbstractMessage cm in mergeMessage.msgs)
                {
                    Logger().LogDebug(cm.ToString());
                }
                var sb = new StringBuilder();
                foreach (long l in mergeMessage.msgIds)
                {
                    sb.Append(MSG_ID_PREFIX).Append(l).Append(SINGLE_LOG_POSTFIX);
                }
                sb.Append('\n');
                foreach (long l in outerInstance._futures.Keys)
                {
                    sb.Append(FUTURES_PREFIX).Append(l).Append(SINGLE_LOG_POSTFIX);
                }
                Logger().LogDebug(sb.ToString());
            }
        }
    }
    /// <summary>
    /// The type ClientHandler.
    /// </summary>
    internal class ClientHandler : ChannelDuplexHandler
    {
        public override bool IsSharable => true;

        private readonly AbstractNettyRemotingClient _outerInstance;
        public ClientHandler(AbstractNettyRemotingClient outerInstance)
        {
            _outerInstance = outerInstance;
        }

        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            if (!(msg is RpcMessage))
            {
                return;
            }
            _outerInstance.ProcessMessage(ctx, (RpcMessage)msg);
        }

        public override void ChannelWritabilityChanged(IChannelHandlerContext ctx)
        {
            lock (_outerInstance.@lock)
            {
                if (ctx.Channel.IsWritable)
                {
                    Monitor.PulseAll(_outerInstance.@lock);
                }
            }
            ctx.FireChannelWritabilityChanged();
        }

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            if (_outerInstance.messageExecutor.IsShutdown)
            {
                return;
            }
            if (Logger().IsEnabled(LogLevel.Information))
            {
                Logger().LogInformation($"channel inactive: {ctx.Channel}");
            }
            _outerInstance.clientChannelManager.ReleaseChannel(ctx.Channel, Utils.NetUtil.ToStringAddress(ctx.Channel.RemoteAddress)).GetAwaiter().GetResult();
            base.ChannelInactive(ctx);
        }

        public override void UserEventTriggered(IChannelHandlerContext ctx, object evt)
        {
            if (evt is IdleStateEvent idleStateEvent)
            {
                if (idleStateEvent.State == IdleState.ReaderIdle)
                {
                    if (Logger().IsEnabled(LogLevel.Information))
                    {
                        Logger().LogInformation($"channel {ctx.Channel} read idle.");
                    }
                    try
                    {
                        string serverAddress = Utils.NetUtil.ToStringAddress(ctx.Channel.RemoteAddress);
                        _outerInstance.clientChannelManager.InvalidateObject(serverAddress, ctx.Channel).GetAwaiter().GetResult();
                    }
                    catch (Exception exx)
                    {
                        Logger().LogError(exx, exx.Message);
                    }
                    finally
                    {
                        _outerInstance.clientChannelManager.ReleaseChannel(ctx.Channel, _outerInstance.GetAddressFromContext(ctx)).GetAwaiter().GetResult();
                    }
                }
                if (idleStateEvent == IdleStateEvent.WriterIdleStateEvent)
                {
                    try
                    {
                        if (Logger().IsEnabled(LogLevel.Debug))
                        {
                            Logger().LogDebug($"will send ping msg,channel {ctx.Channel}");
                        }
                        _outerInstance.SendAsyncRequest(ctx.Channel, HeartbeatMessage.PING).GetAwaiter().GetResult();
                    }
                    catch (Exception throwable)
                    {
                        Logger().LogError(throwable, $"send request error: {throwable.Message}");
                    }
                }
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception cause)
        {
            Logger().LogError(cause, $"{FrameworkErrorCode.ExceptionCaught.GetErrCode()}:{Utils.NetUtil.ToStringAddress(ctx.Channel.RemoteAddress)} connect exception.{cause.Message}");
            _outerInstance.clientChannelManager.ReleaseChannel(ctx.Channel, ChannelUtil.GetAddressFromChannel(ctx.Channel)).GetAwaiter().GetResult();
            if (Logger().IsEnabled(LogLevel.Information))
            {
                Logger().LogInformation($"remove exception rm channel:{ctx.Channel}");
            }
            base.ExceptionCaught(ctx, cause);
        }

        public override async Task CloseAsync(IChannelHandlerContext ctx)
        {
            if (Logger().IsEnabled(LogLevel.Information))
            {
                Logger().LogInformation($"{ctx} will closed");
            }
            await base.CloseAsync(ctx);
        }
    }
}
