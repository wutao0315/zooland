using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using ZooTa.Atomic;
using ZooTa.Concurrency;
using Zooyard.Rpc.Auth;
using Zooyard.Rpc.Constant;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.Rpc.Processor.Client;
using ZooTa.Exceptions;
using ZooTa.Loader;
using ZooTa.Logging;
using ZooTa.Utils;

namespace Zooyard.Rpc.NettyImpl.Support
{
    /// <summary>
    /// The type Rpc client.
    /// </summary>
    public sealed class TmNettyRemotingClient : AbstractNettyRemotingClient
	{
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(TmNettyRemotingClient));
        private static volatile TmNettyRemotingClient instance;
        //private static readonly long KEEP_ALIVE_TIME = int.MaxValue;
        //private const int MAX_QUEUE_SIZE = 2000;
        private readonly AtomicBoolean initialized = new (false);
        private string applicationId;
        private string transactionServiceGroup;
        private readonly IAuthSigner signer;
        private string accessKey;
        private string secretKey;

        /// <summary>
		/// The constant enableDegrade.
		/// </summary>
		public static bool enableDegrade = false;
        private TmNettyRemotingClient(NettyClientConfig nettyClientConfig,
            IEventExecutorGroup eventExecutorGroup,
            //ThreadPoolExecutor messageExecutor)
            MultithreadEventLoopGroup messageExecutor)
            : base(nettyClientConfig, 
                  eventExecutorGroup,
                  messageExecutor,
                  NettyPoolKey.TransactionRole.TMROLE)
        {
            this.signer = EnhancedServiceLoader.Load<IAuthSigner>();
        }

        /// <summary>
        /// Gets instance.
        /// </summary>
        /// <param name="applicationId">           the application id </param>
        /// <param name="transactionServiceGroup"> the transaction service group </param>
        /// <returns> the instance </returns>
        public static TmNettyRemotingClient GetInstance(string applicationId, string transactionServiceGroup, string accessKey, string secretKey)
        {
            TmNettyRemotingClient tmRpcClient = Instance;
            tmRpcClient.ApplicationId = applicationId;
            tmRpcClient.transactionServiceGroup = transactionServiceGroup;
            tmRpcClient.AccessKey = accessKey;
            tmRpcClient.SecretKey = secretKey;
            return tmRpcClient;
        }

        /// <summary>
        /// Gets instance.
        /// </summary>
        /// <returns> the instance </returns>
        public static TmNettyRemotingClient Instance
        {
            get
            {
                if (null == instance)
                {
                    lock (typeof(TmNettyRemotingClient))
                    {
                        if (null == instance)
                        {
                            NettyClientConfig nettyClientConfig = new();
                            //ThreadPoolExecutor threadPoolExecutor = new(nettyClientConfig.ClientWorkerThreads, nettyClientConfig.ClientWorkerThreads,
                            //    TimeSpan.FromMilliseconds(KEEP_ALIVE_TIME),
                            //    new BlockingCollection<object>(MAX_QUEUE_SIZE),
                            //    new NamedThreadFactory(nettyClientConfig.TmDispatchThreadPrefix, nettyClientConfig.ClientWorkerThreads));//, RejectedPolicies.runsOldestTaskPolicy());
                            var threadPoolExecutor = new MultithreadEventLoopGroup(nettyClientConfig.ClientWorkerThreads);
                            instance = new TmNettyRemotingClient(nettyClientConfig, null, threadPoolExecutor);
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
        /// <summary>
        /// Sets access key.
        /// </summary>
        /// <param name="accessKey"> the access key </param>
        internal string AccessKey
        {
            set
            {
                if (null != value)
                {
                    this.accessKey = value;
                    return;
                }
                this.accessKey = SystemPropertyUtil.Get(ConfigurationKeys.SEATA_ACCESS_KEY);
            }
        }

        /// <summary>
        /// Sets secret key.
        /// </summary>
        /// <param name="secretKey"> the secret key </param>
        internal string SecretKey
        {
            set
            {
                if (null != value)
                {
                    this.secretKey = value;
                    return;
                }
                this.secretKey = SystemPropertyUtil.Get(ConfigurationKeys.SEATA_SECRET_KEY);
            }
        }

        public override async Task Init()
        {
            // registry processor
            await RegisterProcessor();
            if (initialized.CompareAndSet(false, true))
            {
                await base.Init();
            }
        }


        public override async Task OnRegisterMsgSuccess(string serverAddress, IChannel channel, object response, AbstractMessage requestMessage)
        {
            var registerTMRequest = (RegisterTMRequest)requestMessage;
            var registerTMResponse = (RegisterTMResponse)response;
            if (Logger().IsEnabled(LogLevel.Information))
            {
                Logger().LogInformation($"register TM success. client version:{registerTMRequest.Version}, server version:{registerTMResponse.Version},channel:{channel}");
            }
            ClientChannelManager.RegisterChannel(serverAddress, channel);
            await Task.CompletedTask;
        }

        public override async Task OnRegisterMsgFail(string serverAddress, IChannel channel, object response, AbstractMessage requestMessage)
        {
            var registerTMRequest = (RegisterTMRequest)requestMessage;
            var registerTMResponse = (RegisterTMResponse)response;
            string errMsg = $"register TM failed. client version: {registerTMRequest.Version},server version: {registerTMResponse?.Version}, errorMsg: {registerTMResponse?.Msg}, channel: {channel}";
            await Task.CompletedTask;
            throw new FrameworkException(errMsg);
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
                return severAddress =>
                {
                    RegisterTMRequest message = new(applicationId, transactionServiceGroup, ExtraData);
                    return new NettyPoolKey(NettyPoolKey.TransactionRole.TMROLE, severAddress, message);
                };
            }
        }

        private async Task RegisterProcessor()
        {
            // 1.registry TC response processor
            var onResponseProcessor = new ClientOnResponseProcessor(mergeMsgMap, base.Futures, TransactionMessageHandler);
            await base.RegisterProcessor(MessageType.TYPE_SEATA_MERGE_RESULT, onResponseProcessor, null);
            await base.RegisterProcessor(MessageType.TYPE_GLOBAL_BEGIN_RESULT, onResponseProcessor, null);
            await base.RegisterProcessor(MessageType.TYPE_GLOBAL_COMMIT_RESULT, onResponseProcessor, null);
            await base.RegisterProcessor(MessageType.TYPE_GLOBAL_REPORT_RESULT, onResponseProcessor, null);
            await base.RegisterProcessor(MessageType.TYPE_GLOBAL_ROLLBACK_RESULT, onResponseProcessor, null);
            await base.RegisterProcessor(MessageType.TYPE_GLOBAL_STATUS_RESULT, onResponseProcessor, null);
            await base.RegisterProcessor(MessageType.TYPE_REG_CLT_RESULT, onResponseProcessor, null);
            // 2.registry heartbeat message processor
            var clientHeartbeatProcessor = new ClientHeartbeatProcessor();
            await base.RegisterProcessor(MessageType.TYPE_HEARTBEAT_MSG, clientHeartbeatProcessor, null);
        }

        private string ExtraData
        {
            get
            {
                string ip = NetUtil.LocalIp;
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string digestSource;
                if (string.IsNullOrWhiteSpace(ip))
                {
                    digestSource = TransactionServiceGroup + ",127.0.0.1," + timestamp;
                }
                else
                {
                    digestSource = TransactionServiceGroup + "," + ip + "," + timestamp;
                }
                string digest = signer.Sign(digestSource, secretKey);
                StringBuilder sb = new();
                sb.Append(RegisterTMRequest.UDATA_AK).Append(ConfigurationKeys.EXTRA_DATA_KV_CHAR).Append(accessKey).Append(ConfigurationKeys.EXTRA_DATA_SPLIT_CHAR);
                sb.Append(RegisterTMRequest.UDATA_DIGEST).Append(ConfigurationKeys.EXTRA_DATA_KV_CHAR).Append(digest).Append(ConfigurationKeys.EXTRA_DATA_SPLIT_CHAR);
                sb.Append(RegisterTMRequest.UDATA_TIMESTAMP).Append(ConfigurationKeys.EXTRA_DATA_KV_CHAR).Append(timestamp).Append(ConfigurationKeys.EXTRA_DATA_SPLIT_CHAR);
                return sb.ToString();
            }
        }
    }
}