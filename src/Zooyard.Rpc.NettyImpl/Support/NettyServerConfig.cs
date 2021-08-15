using Microsoft.Extensions.Configuration;
using System;
using Zooyard.Rpc.NettyImpl.Constant;
using Zooyard.Utils;


namespace Zooyard.Rpc.NettyImpl.Support
{
    /// <summary>
    /// The type Netty server config.
    /// 
    /// @author slievrly
    /// </summary>
    public class NettyServerConfig : NettyBaseConfig
	{
		private int serverSelectorThreads = SystemPropertyUtil.GetInt(ConfigurationKeys.TRANSPORT_PREFIX + "serverSelectorThreads", WORKER_THREAD_SIZE);
		private int serverSocketSendBufSize = SystemPropertyUtil.GetInt(ConfigurationKeys.TRANSPORT_PREFIX + "serverSocketSendBufSize", 153600);
		private int serverSocketResvBufSize = SystemPropertyUtil.GetInt(ConfigurationKeys.TRANSPORT_PREFIX + "serverSocketResvBufSize", 153600);
		private int serverWorkerThreads = SystemPropertyUtil.GetInt(ConfigurationKeys.TRANSPORT_PREFIX + "serverWorkerThreads", WORKER_THREAD_SIZE);
		private int soBackLogSize = SystemPropertyUtil.GetInt(ConfigurationKeys.TRANSPORT_PREFIX + "soBackLogSize", 1024);
		private int writeBufferHighWaterMark = SystemPropertyUtil.GetInt(ConfigurationKeys.TRANSPORT_PREFIX + "writeBufferHighWaterMark", 67108864);
		private int writeBufferLowWaterMark = SystemPropertyUtil.GetInt(ConfigurationKeys.TRANSPORT_PREFIX + "writeBufferLowWaterMark", 1048576);
		private const int DEFAULT_LISTEN_PORT = 8091;
		private const int RPC_REQUEST_TIMEOUT = 30 * 1000;
		private int serverChannelMaxIdleTimeSeconds = SystemPropertyUtil.GetInt(ConfigurationKeys.TRANSPORT_PREFIX + "serverChannelMaxIdleTimeSeconds", 30);
		private const string EPOLL_WORKER_THREAD_PREFIX = "NettyServerEPollWorker";
		private static int minServerPoolSize = SystemPropertyUtil.GetInt(ConfigurationKeys.MIN_SERVER_POOL_SIZE, 50);
		private static int maxServerPoolSize = SystemPropertyUtil.GetInt(ConfigurationKeys.MAX_SERVER_POOL_SIZE, 500);
		private static int maxTaskQueueSize = SystemPropertyUtil.GetInt(ConfigurationKeys.MAX_TASK_QUEUE_SIZE, 20000);
		private static int keepAliveTime = SystemPropertyUtil.GetInt(ConfigurationKeys.KEEP_ALIVE_TIME, 500);

		/// <summary>
		/// The Server channel clazz.
		/// </summary>
		public new static readonly Type SERVER_CHANNEL_CLAZZ = NettyBaseConfig.SERVER_CHANNEL_CLAZZ;


		/// <summary>
		/// Gets server selector threads.
		/// </summary>
		/// <returns> the server selector threads </returns>
		public virtual int ServerSelectorThreads
		{
			get
			{
				return serverSelectorThreads;
			}
			set
			{
				this.serverSelectorThreads = value;
			}
		}


		/// <summary>
		/// Enable epoll boolean.
		/// </summary>
		/// <returns> the boolean </returns>
		public static bool enableEpoll()
		{
			return false;
			//return NettyBaseConfig.SERVER_CHANNEL_CLAZZ.Equals(typeof(EpollServerSocketChannel)) && Epoll.Available;
		}

		/// <summary>
		/// Gets server socket send buf size.
		/// </summary>
		/// <returns> the server socket send buf size </returns>
		public virtual int ServerSocketSendBufSize
		{
			get
			{
				return serverSocketSendBufSize;
			}
			set
			{
				this.serverSocketSendBufSize = value;
			}
		}


		/// <summary>
		/// Gets server socket resv buf size.
		/// </summary>
		/// <returns> the server socket resv buf size </returns>
		public virtual int ServerSocketResvBufSize
		{
			get
			{
				return serverSocketResvBufSize;
			}
			set
			{
				this.serverSocketResvBufSize = value;
			}
		}


		/// <summary>
		/// Gets server worker threads.
		/// </summary>
		/// <returns> the server worker threads </returns>
		public virtual int ServerWorkerThreads
		{
			get
			{
				return serverWorkerThreads;
			}
			set
			{
				this.serverWorkerThreads = value;
			}
		}


		/// <summary>
		/// Gets so back log size.
		/// </summary>
		/// <returns> the so back log size </returns>
		public virtual int SoBackLogSize
		{
			get
			{
				return soBackLogSize;
			}
			set
			{
				this.soBackLogSize = value;
			}
		}


		/// <summary>
		/// Gets write buffer high water mark.
		/// </summary>
		/// <returns> the write buffer high water mark </returns>
		public virtual int WriteBufferHighWaterMark
		{
			get
			{
				return writeBufferHighWaterMark;
			}
			set
			{
				this.writeBufferHighWaterMark = value;
			}
		}


		/// <summary>
		/// Gets write buffer low water mark.
		/// </summary>
		/// <returns> the write buffer low water mark </returns>
		public virtual int WriteBufferLowWaterMark
		{
			get
			{
				return writeBufferLowWaterMark;
			}
			set
			{
				this.writeBufferLowWaterMark = value;
			}
		}


		/// <summary>
		/// Gets listen port.
		/// </summary>
		/// <returns> the listen port </returns>
		public virtual int DefaultListenPort
		{
			get
			{
				return DEFAULT_LISTEN_PORT;
			}
		}

		/// <summary>
		/// Gets channel max read idle seconds.
		/// </summary>
		/// <returns> the channel max read idle seconds </returns>
		public virtual int ChannelMaxReadIdleSeconds
		{
			get
			{
				return MAX_READ_IDLE_SECONDS;
			}
		}

		/// <summary>
		/// Gets server channel max idle time seconds.
		/// </summary>
		/// <returns> the server channel max idle time seconds </returns>
		public virtual int ServerChannelMaxIdleTimeSeconds
		{
			get
			{
				return serverChannelMaxIdleTimeSeconds;
			}
		}

		/// <summary>
		/// Gets rpc request timeout.
		/// </summary>
		/// <returns> the rpc request timeout </returns>
		public static int RpcRequestTimeout
		{
			get
			{
				return RPC_REQUEST_TIMEOUT;
			}
		}

		/// <summary>
		/// Get boss thread prefix string.
		/// </summary>
		/// <returns> the string </returns>
		public virtual string BossThreadPrefix
		{
			get
			{
				return CONFIG.GetValue(ConfigurationKeys.BOSS_THREAD_PREFIX, DefaultValues.DEFAULT_BOSS_THREAD_PREFIX);
			}
		}

		/// <summary>
		/// Get worker thread prefix string.
		/// </summary>
		/// <returns> the string </returns>
		public virtual string WorkerThreadPrefix
		{
			get
			{
				return CONFIG.GetValue(ConfigurationKeys.WORKER_THREAD_PREFIX, enableEpoll() ? EPOLL_WORKER_THREAD_PREFIX : DefaultValues.DEFAULT_NIO_WORKER_THREAD_PREFIX);
			}
		}

		/// <summary>
		/// Get executor thread prefix string.
		/// </summary>
		/// <returns> the string </returns>
		public virtual string ExecutorThreadPrefix
		{
			get
			{
				return CONFIG.GetValue(ConfigurationKeys.SERVER_EXECUTOR_THREAD_PREFIX, DefaultValues.DEFAULT_EXECUTOR_THREAD_PREFIX);
			}
		}

		/// <summary>
		/// Get boss thread size int.
		/// </summary>
		/// <returns> the int </returns>
		public virtual int BossThreadSize
		{
			get
			{
				return CONFIG.GetValue(ConfigurationKeys.BOSS_THREAD_SIZE, DefaultValues.DEFAULT_BOSS_THREAD_SIZE);
			}
		}

		/// <summary>
		/// Get the timeout seconds of shutdown.
		/// </summary>
		/// <returns> the int </returns>
		public virtual int ServerShutdownWaitTime
		{
			get
			{
				return CONFIG.GetValue(ConfigurationKeys.SHUTDOWN_WAIT, DefaultValues.DEFAULT_SHUTDOWN_TIMEOUT_SEC);
			}
		}

		public static int MinServerPoolSize
		{
			get
			{
				return minServerPoolSize;
			}
		}

		public static int MaxServerPoolSize
		{
			get
			{
				return maxServerPoolSize;
			}
		}

		public static int MaxTaskQueueSize
		{
			get
			{
				return maxTaskQueueSize;
			}
		}

		public static int KeepAliveTime
		{
			get
			{
				return keepAliveTime;
			}
		}
	}
}