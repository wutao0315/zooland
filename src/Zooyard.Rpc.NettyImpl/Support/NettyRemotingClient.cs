using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using Zooyard.Atomic;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Processor.Client;
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Support;

/// <summary>
/// The type Rm rpc client.
/// 
/// </summary>
public sealed class NettyRemotingClient : AbstractNettyRemotingClient
	{

    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyRemotingClient));

    private static volatile NettyRemotingClient instance;
    private readonly AtomicBoolean initialized = new (false);
    //private static readonly long KEEP_ALIVE_TIME = int.MaxValue;
    //private const int MAX_QUEUE_SIZE = 20000;
    private string applicationId;
    private string transactionServiceGroup;

    public override async Task Init()
    {
        // registry processor
        await RegisterProcessor();
        if (initialized.CompareAndSet(false, true))
        {
            await base.Init();

            // Found one or more resources that were registered before initialization
            if (!string.IsNullOrWhiteSpace(transactionServiceGroup))
            {
                await ClientChannelManager.Reconnect(transactionServiceGroup);
            }
        }
    }

    public NettyRemotingClient(NettyClientConfig nettyClientConfig,
       IEventExecutorGroup eventExecutorGroup,
       MultithreadEventLoopGroup messageExecutor)
       : base(nettyClientConfig,
             eventExecutorGroup,
             messageExecutor)
    {
    }

    /// <summary>
    /// Gets instance.
    /// </summary>
    /// <param name="applicationId"> the application id </param>
    /// <param name="transactionServiceGroup"> the transaction service group </param>
    /// <returns> the instance </returns>
    public static NettyRemotingClient GetInstance(string applicationId, string transactionServiceGroup)
    {
        NettyRemotingClient rmRpcClient = Instance;
        rmRpcClient.ApplicationId = applicationId;
        rmRpcClient.transactionServiceGroup = transactionServiceGroup;
        return rmRpcClient;
    }

    /// <summary>
    /// Gets instance.
    /// </summary>
    /// <returns> the instance </returns>
    public static NettyRemotingClient Instance
    {
        get
        {
            if (instance == null)
            {
                lock (typeof(NettyRemotingClient))
                {
                    if (instance == null)
                    {
                        var nettyClientConfig = new NettyClientConfig();
                        var threadPoolExecutor = new MultithreadEventLoopGroup(nettyClientConfig.ClientWorkerThreads);
                        instance = new NettyRemotingClient(nettyClientConfig, null, threadPoolExecutor);
                    }
                }
            }
            return instance;
        }
    }

    /// <summary>
		/// Sets application id.
		/// </summary>
		/// <param name="applicationId"> the application id </param>
		public string ApplicationId
    {
        set
        {
            this.applicationId = value;
        }
    }

    /// <summary>
    /// Sets transaction service group.
    /// </summary>
    /// <param name="transactionServiceGroup"> the transaction service group </param>
    protected internal override string TransactionServiceGroup
    {
        get
        {
            return transactionServiceGroup;
        }
    }


    public override async Task OnRegisterMsgSuccess(string serverAddress, IChannel channel, object response, AbstractMessage requestMessage)
    {
        await Task.CompletedTask;
        //var registerRMRequest = (RegisterRMRequest)requestMessage;
        //var registerRMResponse = (RegisterRMResponse)response;
        //if (Logger().IsEnabled(LogLevel.Information))
        //{
        //    Logger().LogInformation($"register RM success. client version:{registerRMRequest.Version}, server version:{registerRMResponse.Version},channel:{channel}");
        //}
        //ClientChannelManager.RegisterChannel(serverAddress, channel);
        //string dbKey = MergedResourceKeys;
        //if (!string.ReferenceEquals(registerRMRequest.ResourceIds, null))
        //{
        //    if (!registerRMRequest.ResourceIds.Equals(dbKey))
        //    {
        //        await SendRegisterMessage(serverAddress, channel, dbKey);
        //    }
        //}

    }

    public override async Task OnRegisterMsgFail(string serverAddress, IChannel channel, object response, AbstractMessage requestMessage)
    {
        await Task.CompletedTask;
        //var registerRMRequest = (RegisterRMRequest)requestMessage;
        //var registerRMResponse = (RegisterRMResponse)response;
        //string errMsg = $"register RM failed. client version: {registerRMRequest.Version},server version: {registerRMResponse?.Version}, errorMsg: {registerRMResponse?.Msg}, channel: {channel}";
        //await Task.CompletedTask;
        //throw new FrameworkException(errMsg);
    }

    /// <summary>
    /// Register new db key.
    /// </summary>
    /// <param name="resourceGroupId"> the resource group id </param>
    /// <param name="resourceId">      the db key </param>
    public async Task RegisterResource(string resourceGroupId, string resourceId)
    {

        // Resource registration cannot be performed until the RM client is initialized
        if (string.IsNullOrWhiteSpace(transactionServiceGroup))
        {
            return;
        }

        if (ClientChannelManager.Channels.IsEmpty)
        {
            await ClientChannelManager.Reconnect(transactionServiceGroup);
            return;
        }
        lock (ClientChannelManager.Channels)
        {
            foreach (KeyValuePair<string, IChannel> entry in ClientChannelManager.Channels)
            {
                string serverAddress = entry.Key;
                IChannel rmChannel = entry.Value;
                if (Logger().IsEnabled(LogLevel.Information))
                {
                    Logger().LogInformation($"will register resourceId:{resourceId}");
                }
                SendRegisterMessage(serverAddress, rmChannel, resourceId).GetAwaiter().GetResult();
            }
        }
        await Task.CompletedTask;
    }

    public async Task SendRegisterMessage(string serverAddress, IChannel channel, string resourceId)
    {
        await Task.CompletedTask;
        //RegisterRMRequest message = new (applicationId, transactionServiceGroup);
        //message.ResourceIds = resourceId;
        //try
        //{
        //    await base.SendAsyncRequest(channel, message);
        //}
        //catch (FrameworkException e)
        //{
        //    if (e.Errcode == FrameworkErrorCode.ChannelIsNotWritable && !string.ReferenceEquals(serverAddress, null))
        //    {
        //        await ClientChannelManager.ReleaseChannel(channel, serverAddress);
        //        if (Logger().IsEnabled(LogLevel.Information))
        //        {
        //            Logger().LogInformation($"remove not writable channel:{channel}");
        //        }
        //    }
        //    else
        //    {
        //        Logger().LogError(e, $"register resource failed, channel:{channel},resourceId:{resourceId}");
        //    }
        //}
    }

    public string MergedResourceKeys
    {
        get
        {
            //IDictionary<string, IResource> managedResources = resourceManager.ManagedResources;
            //ICollection<string> resourceIds = managedResources.Keys;
            //if (resourceIds.Count > 0)
            //{
            //    StringBuilder sb = new ();
            //    bool first = true;
            //    foreach (string resourceId in resourceIds)
            //    {
            //        if (first)
            //        {
            //            first = false;
            //        }
            //        else
            //        {
            //            sb.Append(Constants.DBKEYS_SPLIT_CHAR);
            //        }
            //        sb.Append(resourceId);
            //    }
            //    return sb.ToString();
            //}
            return null;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        initialized.CompareAndSet(false, false);
        instance = null;
    }

    protected internal override Func<string, NettyPoolKey> PoolKeyFunction
    {
        get
        {
            return serverAddress =>
            {
                //string resourceIds = MergedResourceKeys;
                //if (!string.ReferenceEquals(resourceIds, null) && Logger().IsEnabled(LogLevel.Information))
                //{
                //    Logger().LogInformation($"will register :{resourceIds}");
                //}
                return new NettyPoolKey(serverAddress);
            };
        }
    }
    private async Task RegisterProcessor()
    {
        // 1.registry response processor
        var onResponseProcessor = new ClientOnResponseProcessor(mergeMsgMap, base.Futures, TransactionMessageHandler);
        await base.RegisterProcessor(MessageType.TYPE_SEATA_MERGE_RESULT, onResponseProcessor, null);
        await base.RegisterProcessor(MessageType.TYPE_BRANCH_REGISTER_RESULT, onResponseProcessor, null);
        await base.RegisterProcessor(MessageType.TYPE_BRANCH_STATUS_REPORT_RESULT, onResponseProcessor, null);
        await base.RegisterProcessor(MessageType.TYPE_GLOBAL_LOCK_QUERY_RESULT, onResponseProcessor, null);
        await base.RegisterProcessor(MessageType.TYPE_REG_RM_RESULT, onResponseProcessor, null);
        // 2.registry heartbeat message processor
        var clientHeartbeatProcessor = new ClientHeartbeatProcessor();
        await base.RegisterProcessor(MessageType.TYPE_HEARTBEAT_MSG, clientHeartbeatProcessor, null);
    }
}
