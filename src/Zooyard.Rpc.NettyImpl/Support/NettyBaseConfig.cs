using DotNetty.Transport.Channels.Local;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Constant;

namespace Zooyard.Rpc.NettyImpl.Support;

public class NettyTransportBaseConfigOptoin
    {
        public string Type { get; set; } = TransportProtocolType.SOCKET.ToString();
        public string Server { get; set; } = TransportServerType.NIO.ToString();
        public string WorkerThreadSize { get; set; }
        public string BossThreadPrefix { get; set; }
        public string WorkerThreadPrefix { get; set; }
        public bool ShareBossWorker{ get; set; }
        public bool Heartbeat { get; set; } = false;

    }

/// <summary>
/// The type Netty base config.
/// 
/// @author slievrly
/// </summary>
public class NettyBaseConfig
{
	private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyBaseConfig));

	///// <summary>
	///// The constant CONFIG.
	///// </summary>
	//protected internal static readonly IConfiguration CONFIG = ConfigurationFactory.Instance;
	///// <summary>
	///// The constant BOSS_THREAD_PREFIX.
	///// </summary>
	//protected internal static readonly string BOSS_THREAD_PREFIX = CONFIG.GetValue<string>(ConfigurationKeys.BOSS_THREAD_PREFIX);

	///// <summary>
	///// The constant WORKER_THREAD_PREFIX.
	///// </summary>
	//protected internal static readonly string WORKER_THREAD_PREFIX = CONFIG.GetValue<string>(ConfigurationKeys.WORKER_THREAD_PREFIX);

	///// <summary>
	///// The constant SHARE_BOSS_WORKER.
	///// </summary>
	//protected internal static readonly bool SHARE_BOSS_WORKER = CONFIG.GetValue<bool>(ConfigurationKeys.SHARE_BOSS_WORKER);

	/// <summary>
	/// The constant WORKER_THREAD_SIZE.
	/// </summary>
	protected internal static int WORKER_THREAD_SIZE;

	/// <summary>
	/// The constant TRANSPORT_SERVER_TYPE.
	/// </summary>
	protected internal static readonly TransportServerType TRANSPORT_SERVER_TYPE;

	/// <summary>
	/// The constant SERVER_CHANNEL_CLAZZ.
	/// </summary>
	protected internal static readonly Type SERVER_CHANNEL_CLAZZ;
	/// <summary>
	/// The constant CLIENT_CHANNEL_CLAZZ.
	/// </summary>
	protected internal static readonly Type CLIENT_CHANNEL_CLAZZ;

	/// <summary>
	/// The constant TRANSPORT_PROTOCOL_TYPE.
	/// </summary>
	protected internal static readonly TransportProtocolType TRANSPORT_PROTOCOL_TYPE;

	private const int DEFAULT_WRITE_IDLE_SECONDS = 5;

	private const int READIDLE_BASE_WRITEIDLE = 3;


	/// <summary>
	/// The constant MAX_WRITE_IDLE_SECONDS.
	/// </summary>
	protected internal static readonly int MAX_WRITE_IDLE_SECONDS;

	/// <summary>
	/// The constant MAX_READ_IDLE_SECONDS.
	/// </summary>
	protected internal static readonly int MAX_READ_IDLE_SECONDS;

	/// <summary>
	/// The constant MAX_ALL_IDLE_SECONDS.
	/// </summary>
	protected internal const int MAX_ALL_IDLE_SECONDS = 0;

	//static NettyBaseConfig()
	//{
 //           Enum.TryParse(CONFIG.GetValue(ConfigurationKeys.TRANSPORT_TYPE, TransportProtocolType.SOCKET.ToString()),true, out TRANSPORT_PROTOCOL_TYPE);
	//	string workerThreadSize = CONFIG.GetValue<string>(ConfigurationKeys.WORKER_THREAD_SIZE);
	//	if ((!string.IsNullOrWhiteSpace(workerThreadSize)) && int.TryParse(workerThreadSize, out int value))
	//	{
	//		WORKER_THREAD_SIZE = value;
	//	}
	//	else if (Enum.TryParse(workerThreadSize, out WorkThreadMode mode))
	//	{
	//		WORKER_THREAD_SIZE = mode.GetValue();
	//	}
	//	else
	//	{
	//		WORKER_THREAD_SIZE = WorkThreadMode.Default.GetValue();
	//	}

	//	Enum.TryParse(CONFIG.GetValue(ConfigurationKeys.TRANSPORT_SERVER, TransportServerType.NIO.ToString()), true, out TRANSPORT_SERVER_TYPE);

	//	switch (TRANSPORT_SERVER_TYPE)
	//	{
	//		case TransportServerType.NIO:
	//			if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.SOCKET)
	//			{
	//				SERVER_CHANNEL_CLAZZ = typeof(TcpServerSocketChannel);
	//				CLIENT_CHANNEL_CLAZZ = typeof(TcpSocketChannel);
	//			}
	//			else if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.LIBUV)
	//			{
	//				SERVER_CHANNEL_CLAZZ = typeof(TcpServerChannel);
	//				CLIENT_CHANNEL_CLAZZ = typeof(TcpChannel);
	//			}
	//			else if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.LOCAL)
	//			{
	//				SERVER_CHANNEL_CLAZZ = typeof(LocalServerChannel);
	//				CLIENT_CHANNEL_CLAZZ = typeof(LocalChannel);
	//			}
	//			else
	//			{
	//				raiseUnsupportedTransportError();
	//				SERVER_CHANNEL_CLAZZ = null;
	//				CLIENT_CHANNEL_CLAZZ = null;
	//			}
	//			break;
	//		case TransportServerType.NATIVE:
				
	//			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
	//			{
	//				throw new ArgumentException("no native supporting for Windows.");
	//			}
	//			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
	//			{
	//				if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.SOCKET)
	//				{
	//					SERVER_CHANNEL_CLAZZ = typeof(TcpServerSocketChannel);
	//					CLIENT_CHANNEL_CLAZZ = typeof(TcpSocketChannel);
	//				}
	//				else if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.LIBUV)
	//				{
	//					SERVER_CHANNEL_CLAZZ = typeof(TcpServerChannel);
	//					CLIENT_CHANNEL_CLAZZ = typeof(TcpChannel);
	//				}
	//				else if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.LOCAL)
	//				{
	//					SERVER_CHANNEL_CLAZZ = typeof(LocalServerChannel);
	//					CLIENT_CHANNEL_CLAZZ = typeof(LocalChannel);
	//				}
	//				else
	//				{
	//					raiseUnsupportedTransportError();
	//					SERVER_CHANNEL_CLAZZ = null;
	//					CLIENT_CHANNEL_CLAZZ = null;
	//				}
	//			}
	//			else
	//			{
	//				if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.SOCKET)
	//				{
	//					SERVER_CHANNEL_CLAZZ = typeof(TcpServerSocketChannel);
	//					CLIENT_CHANNEL_CLAZZ = typeof(TcpSocketChannel);
	//				}
	//				else if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.LIBUV)
	//				{
	//					SERVER_CHANNEL_CLAZZ = typeof(TcpServerChannel);
	//					CLIENT_CHANNEL_CLAZZ = typeof(TcpChannel);
	//				}
	//				else if (TRANSPORT_PROTOCOL_TYPE == TransportProtocolType.LOCAL)
	//				{
	//					SERVER_CHANNEL_CLAZZ = typeof(LocalServerChannel);
	//					CLIENT_CHANNEL_CLAZZ = typeof(LocalChannel);
	//				}
	//				else
	//				{
	//					raiseUnsupportedTransportError();
	//					SERVER_CHANNEL_CLAZZ = null;
	//					CLIENT_CHANNEL_CLAZZ = null;
	//				}
	//			}
	//			break;
	//		default:
	//			throw new ArgumentException("unsupported.");
	//	}
	//	bool enableHeartbeat = CONFIG.GetValue(ConfigurationKeys.TRANSPORT_HEARTBEAT, DefaultValues.DEFAULT_TRANSPORT_HEARTBEAT);
	//	if (enableHeartbeat)
	//	{
	//		MAX_WRITE_IDLE_SECONDS = DEFAULT_WRITE_IDLE_SECONDS;
	//	}
	//	else
	//	{
	//		MAX_WRITE_IDLE_SECONDS = 0;
	//	}
	//	MAX_READ_IDLE_SECONDS = MAX_WRITE_IDLE_SECONDS * READIDLE_BASE_WRITEIDLE;
	//}

	private static void raiseUnsupportedTransportError()
	{
		string errMsg = $"Unsupported provider type :[{TRANSPORT_SERVER_TYPE}] for transport:[{TRANSPORT_PROTOCOL_TYPE}].";
		Logger().LogError(errMsg);
		throw new ArgumentException(errMsg);
	}

    }
    /// <summary>
    /// The enum Work thread mode.
    /// </summary>
    internal enum WorkThreadMode
    {
        Auto,
        Pin,
        BusyPin,
        Default
    }
    internal static class WorkThreadModeExtensions
    {
        private static readonly int availableProcessors = Environment.ProcessorCount;

        public static int GetValue(this WorkThreadMode mode)
        {
            var result = availableProcessors * 2;
            switch (mode)
            {
                case WorkThreadMode.Auto:
                    result = availableProcessors * 2 + 1;
                    break;
                case WorkThreadMode.Pin:
                    result = availableProcessors;
                    break;
                case WorkThreadMode.BusyPin:
                    result = availableProcessors + 1;
                    break;
                case WorkThreadMode.Default:
                    result = availableProcessors * 2;
                    break;
                default:
                    break;
            }
            return result;
        }
    }
