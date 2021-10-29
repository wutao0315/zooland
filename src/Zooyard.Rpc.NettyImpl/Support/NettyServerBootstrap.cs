using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Atomic;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Support.V1;


namespace Zooyard.Rpc.NettyImpl.Support
{
    /// <summary>
    /// Rpc server bootstrap.
    /// 
    /// </summary>
    public class NettyServerBootstrap : IRemotingBootstrap
	{
		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyServerBootstrap));
		private readonly ServerBootstrap _serverBootstrap = new();
		private readonly IEventLoopGroup _eventLoopGroupBoss;
		private readonly IEventLoopGroup _eventLoopGroupWorker;
		private readonly NettyServerConfig _nettyServerConfig;
		private IChannelHandler[] channelHandlers;
		private int listenPort;
		private readonly AtomicBoolean _initialized = new(false);

		public NettyServerBootstrap(NettyServerConfig nettyServerConfig)
		{
			_nettyServerConfig = nettyServerConfig;
			_eventLoopGroupBoss = new MultithreadEventLoopGroup(nettyServerConfig.BossThreadSize);
			_eventLoopGroupWorker = new MultithreadEventLoopGroup(nettyServerConfig.ServerWorkerThreads);
		}

		/// <summary>
		/// Sets channel handlers.
		/// </summary>
		/// <param name="handlers"> the handlers </param>
		protected internal virtual void SetChannelHandlers(params IChannelHandler[] value)
		{
			if (value != null)
			{
				channelHandlers = value;
			}
		}

		/// <summary>
		/// Add channel pipeline last.
		/// </summary>
		/// <param name="channel">  the channel </param>
		/// <param name="handlers"> the handlers </param>
		private void AddChannelPipelineLast(IChannel channel, params IChannelHandler[] handlers)
		{
			if (channel != null && handlers != null)
			{
				channel.Pipeline.AddLast(handlers);
			}
		}

		/// <summary>
		/// Sets listen port.
		/// </summary>
		/// <param name="listenPort"> the listen port </param>
		public virtual int ListenPort
		{
			get
			{
				return listenPort;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentException($"listen port: {value} is invalid!");
				}
				listenPort = value;
			}
		}


		public virtual async Task Start()
		{
			_serverBootstrap.Group(_eventLoopGroupBoss, _eventLoopGroupWorker)
				.ChannelFactory(() =>
				{
					IServerChannel channel = NettyServerConfig.SERVER_CHANNEL_CLAZZ.GetConstructor(Array.Empty<Type>())
                   .Invoke(Array.Empty<object>()) as IServerChannel;
					return channel;
				})
				.Option(ChannelOption.SoBacklog, _nettyServerConfig.SoBackLogSize)
				.Option(ChannelOption.SoReuseaddr, true)
				.ChildOption(ChannelOption.SoKeepalive, true)
				.ChildOption(ChannelOption.TcpNodelay, true)
				.ChildOption(ChannelOption.SoSndbuf, _nettyServerConfig.ServerSocketSendBufSize)
				.ChildOption(ChannelOption.SoRcvbuf, _nettyServerConfig.ServerSocketResvBufSize)
				.ChildOption(ChannelOption.WriteBufferLowWaterMark, _nettyServerConfig.WriteBufferLowWaterMark)
				.ChildOption(ChannelOption.WriteBufferHighWaterMark, _nettyServerConfig.WriteBufferHighWaterMark)
				.LocalAddress(ListenPort)
				.ChildHandler(new ChannelInitializerAnonymousInnerClass(this));

			try
			{
				Logger().LogInformation($"Server starting,listen port: {ListenPort} ... ");
				await _serverBootstrap.BindAsync(IPAddress.Any, ListenPort);
				Logger().LogInformation($"Server started,listen port: {ListenPort} ... ");
				//RegistryFactory.Instance.Register(new IPEndPoint(IPAddress.Parse(XID.IpAddress), XID.Port));
				_initialized.Value = true;
			}
			catch
			{
				throw;
			}
		}

		private class ChannelInitializerAnonymousInnerClass : ChannelInitializer<IChannel>
		{
			private readonly NettyServerBootstrap outerInstance;

			public ChannelInitializerAnonymousInnerClass(NettyServerBootstrap outerInstance)
			{
				this.outerInstance = outerInstance;
			}
			protected override void InitChannel(IChannel ch)
			{
				ch.Pipeline.AddLast(new IdleStateHandler(outerInstance._nettyServerConfig.ChannelMaxReadIdleSeconds, 0, 0))
					.AddLast(new ProtocolV1Decoder())
					.AddLast(new ProtocolV1Encoder());
				if (outerInstance.channelHandlers != null)
				{
					outerInstance.AddChannelPipelineLast(ch, outerInstance.channelHandlers);
				}

			}
		}

		public virtual async Task Shutdown()
		{
			try
			{
				if (Logger().IsEnabled(LogLevel.Debug)) 
				{
					Logger().LogDebug("Shuting server down. ");
				}
				if (_initialized.Value)
				{
					//RegistryFactory.Instance.UnRegister(new IPEndPoint(IPAddress.Parse(XID.IpAddress), XID.Port));
					//RegistryFactory.Instance.Close();
					////wait a few seconds for server transport
					Thread.Sleep(TimeSpan.FromSeconds(_nettyServerConfig.ServerShutdownWaitTime));
				}

				await _eventLoopGroupBoss.ShutdownGracefullyAsync();
				await _eventLoopGroupWorker.ShutdownGracefullyAsync();
			}
			catch (Exception exx)
			{
				Logger().LogError(exx, exx.Message);
			}
		}
	}

}