using Microsoft.Extensions.Configuration;
using Zooyard.Rpc.NettyImpl.Constant;

namespace Zooyard.Rpc.NettyImpl.Support;

/// <summary>
/// The type Netty client config.
/// 
/// </summary>
public class NettyClientConfig : NettyBaseConfig
{
	//private int connectTimeoutMillis = 10000;
	//private int clientSocketSndBufSize = 153600;
	//private int clientSocketRcvBufSize = 153600;
	//private int clientWorkerThreads = WORKER_THREAD_SIZE;
	private readonly Type clientChannelClazz = CLIENT_CHANNEL_CLAZZ;
	private int perHostMaxConn = 2;
	private const int PER_HOST_MIN_CONN = 2;
	//private int pendingConnSize = int.MaxValue;
	private const int RPC_REQUEST_TIMEOUT = 30 * 1000;
	//private static string vgroup;
	//private static string clientAppName;
	//private static int clientType;
	private static int maxInactiveChannelCheck = 10;
	private const int MAX_NOT_WRITEABLE_RETRY = 2000;
	private const int MAX_CHECK_ALIVE_RETRY = 300;
	private const int CHECK_ALIVE_INTERVAL = 10;
	private const string SOCKET_ADDRESS_START_CHAR = "/";
	private static readonly long MAX_ACQUIRE_CONN_MILLS = 60 * 1000L;
	private const string RPC_DISPATCH_THREAD_PREFIX = "rpcDispatch";
	private const int DEFAULT_MAX_POOL_ACTIVE = 1;
	private const int DEFAULT_MIN_POOL_IDLE = 0;
	private const bool DEFAULT_POOL_TEST_BORROW = true;
	private const bool DEFAULT_POOL_TEST_RETURN = true;
	private const bool DEFAULT_POOL_LIFO = true;
	private static readonly bool ENABLE_CLIENT_BATCH_SEND_REQUEST = GetRequest();

	private static bool GetRequest() 
	{
		var str = Environment.GetEnvironmentVariable(ConfigurationKeys.ENABLE_CLIENT_BATCH_SEND_REQUEST);
		if (bool.TryParse(str, out bool size))
		{
			return size;
		}
		return DefaultValues.DEFAULT_ENABLE_CLIENT_BATCH_SEND_REQUEST;
		//CONFIG.GetValue(ConfigurationKeys.ENABLE_CLIENT_BATCH_SEND_REQUEST, DefaultValues.DEFAULT_ENABLE_CLIENT_BATCH_SEND_REQUEST)
	}
	/// <summary>
	/// Gets connect timeout millis.
	/// </summary>
	/// <returns> the connect timeout millis </returns>
	public virtual int ConnectTimeoutMillis { get; set; } = 10000;


	/// <summary>
	/// Gets client socket snd buf size.
	/// </summary>
	/// <returns> the client socket snd buf size </returns>
	public virtual int ClientSocketSndBufSize { get; set; } = 153600;


	/// <summary>
	/// Gets client socket rcv buf size.
	/// </summary>
	/// <returns> the client socket rcv buf size </returns>
	public virtual int ClientSocketRcvBufSize { get; set; } = 153600;


	/// <summary>
	/// Gets client channel max idle time seconds.
	/// </summary>
	/// <returns> the client channel max idle time seconds </returns>
	public virtual int ChannelMaxWriteIdleSeconds => MAX_WRITE_IDLE_SECONDS;

	/// <summary>
	/// Gets channel max read idle seconds.
	/// </summary>
	/// <returns> the channel max read idle seconds </returns>
	public virtual int ChannelMaxReadIdleSeconds => MAX_READ_IDLE_SECONDS;

	/// <summary>
	/// Gets channel max all idle seconds.
	/// </summary>
	/// <returns> the channel max all idle seconds </returns>
	public virtual int ChannelMaxAllIdleSeconds => MAX_ALL_IDLE_SECONDS;

	/// <summary>
	/// Gets client worker threads.
	/// </summary>
	/// <returns> the client worker threads </returns>
	public virtual int ClientWorkerThreads { get; set; } = WORKER_THREAD_SIZE;


	/// <summary>
	/// Gets client channel clazz.
	/// </summary>
	/// <returns> the client channel clazz </returns>
	public virtual Type ClientChannelClazz => clientChannelClazz;

	/// <summary>
	/// Enable native boolean.
	/// </summary>
	/// <returns> the boolean </returns>
	public virtual bool EnableNative()
	{
		return TRANSPORT_SERVER_TYPE == TransportServerType.NATIVE;
	}

	/// <summary>
	/// Gets per host max conn.
	/// </summary>
	/// <returns> the per host max conn </returns>
	public virtual int PerHostMaxConn
	{
		get
		{
			return perHostMaxConn;
		}
		set
		{
			if (value > PER_HOST_MIN_CONN)
			{
				this.perHostMaxConn = value;
			}
			else
			{
				this.perHostMaxConn = PER_HOST_MIN_CONN;
			}
		}
	}

	/// <summary>
	/// Gets pending conn size.
	/// </summary>
	/// <returns> the pending conn size </returns>
	public virtual int PendingConnSize { get; set; } = int.MaxValue;

	/// <summary>
	/// Gets rpc sendAsyncRequestWithResponse time out.
	/// </summary>
	/// <returns> the rpc sendAsyncRequestWithResponse time out </returns>
	public static int RpcRequestTimeout => RPC_REQUEST_TIMEOUT;

	/// <summary>
	/// Gets vgroup.
	/// </summary>
	/// <returns> the vgroup </returns>
	public static string Vgroup { get; set; }
	/// <summary>
	/// Gets client app name.
	/// </summary>
	/// <returns> the client app name </returns>
	public static string ClientAppName { get; set; }
	/// <summary>
	/// Gets client type.
	/// </summary>
	/// <returns> the client type </returns>
	public static int ClientType { get; set; }

	/// <summary>
	/// Gets max inactive channel check.
	/// </summary>
	/// <returns> the max inactive channel check </returns>
	public static int MaxInactiveChannelCheck => maxInactiveChannelCheck;

	/// <summary>
	/// Gets max not writeable retry.
	/// </summary>
	/// <returns> the max not writeable retry </returns>
	public static int MaxNotWriteableRetry => MAX_NOT_WRITEABLE_RETRY;

	/// <summary>
	/// Gets per host min conn.
	/// </summary>
	/// <returns> the per host min conn </returns>
	public static int PerHostMinConn => PER_HOST_MIN_CONN;

	/// <summary>
	/// Gets max check alive retry.
	/// </summary>
	/// <returns> the max check alive retry </returns>
	public static int MaxCheckAliveRetry => MAX_CHECK_ALIVE_RETRY;

	/// <summary>
	/// Gets check alive interval.
	/// </summary>
	/// <returns> the check alive interval </returns>
	public static int CheckAliveInterval => CHECK_ALIVE_INTERVAL;

	/// <summary>
	/// Gets socket address start char.
	/// </summary>
	/// <returns> the socket address start char </returns>
	public static string SocketAddressStartChar => SOCKET_ADDRESS_START_CHAR;

	/// <summary>
	/// Gets client selector thread size.
	/// </summary>
	/// <returns> the client selector thread size </returns>
	public virtual int ClientSelectorThreadSize
	{
		get
		{
			var sizeStr = Environment.GetEnvironmentVariable(ConfigurationKeys.CLIENT_SELECTOR_THREAD_SIZE);
			if (int.TryParse(sizeStr, out int size))
			{
				return size;
			}
			return DefaultValues.DEFAULT_SELECTOR_THREAD_SIZE;
			//return CONFIG.GetValue(ConfigurationKeys.CLIENT_SELECTOR_THREAD_SIZE, DefaultValues.DEFAULT_SELECTOR_THREAD_SIZE);
		}
	}

	/// <summary>
	/// Get max acquire conn mills long.
	/// </summary>
	/// <returns> the long </returns>
	public virtual long MaxAcquireConnMills => MAX_ACQUIRE_CONN_MILLS;

	/// <summary>
	/// Get client selector thread prefix string.
	/// </summary>
	/// <returns> the string </returns>
	public virtual string ClientSelectorThreadPrefix
	{
		get
		{
			var str = Environment.GetEnvironmentVariable(ConfigurationKeys.CLIENT_SELECTOR_THREAD_PREFIX);
			if (!string.IsNullOrWhiteSpace(str))
			{
				return str;
			}
			return DefaultValues.DEFAULT_SELECTOR_THREAD_PREFIX;
			//return CONFIG.GetValue(ConfigurationKeys.CLIENT_SELECTOR_THREAD_PREFIX, DefaultValues.DEFAULT_SELECTOR_THREAD_PREFIX);
		}
	}

	/// <summary>
	/// Get client worker thread prefix string.
	/// </summary>
	/// <returns> the string </returns>
	public virtual string ClientWorkerThreadPrefix
	{
		get
		{
			var str = Environment.GetEnvironmentVariable(ConfigurationKeys.CLIENT_WORKER_THREAD_PREFIX);
			if (!string.IsNullOrWhiteSpace(str))
			{
				return str;
			}
			return DefaultValues.DEFAULT_WORKER_THREAD_PREFIX;
			//return CONFIG.GetValue(ConfigurationKeys.CLIENT_WORKER_THREAD_PREFIX, DefaultValues.DEFAULT_WORKER_THREAD_PREFIX);
		}
	}

	/// <summary>
	/// Get rpc dispatch thread prefix string.
	/// </summary>
	/// <returns> the string </returns>
	public virtual string RpcDispatchThreadPrefix => RPC_DISPATCH_THREAD_PREFIX;

	/// <summary>
	/// Gets max pool active.
	/// </summary>
	/// <returns> the max pool active </returns>
	public virtual int MaxPoolActive => DEFAULT_MAX_POOL_ACTIVE;

	/// <summary>
	/// Gets min pool idle.
	/// </summary>
	/// <returns> the min pool idle </returns>
	public virtual int MinPoolIdle => DEFAULT_MIN_POOL_IDLE;

	/// <summary>
	/// Is pool test borrow boolean.
	/// </summary>
	/// <returns> the boolean </returns>
	public virtual bool PoolTestBorrow => DEFAULT_POOL_TEST_BORROW;

	/// <summary>
	/// Is pool test return boolean.
	/// </summary>
	/// <returns> the boolean </returns>
	public virtual bool PoolTestReturn => DEFAULT_POOL_TEST_RETURN;

	/// <summary>
	/// Is pool fifo boolean.
	/// </summary>
	/// <returns> the boolean </returns>
	public virtual bool PoolLifo => DEFAULT_POOL_LIFO;

	public static bool EnableClientBatchSendRequest => ENABLE_CLIENT_BATCH_SEND_REQUEST;
}
