using DotNetty.Codecs;
using DotNetty.Common.Concurrency;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using System;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.Processor;
using Zooyard.Logging;
using Zooyard.Utils;

namespace Zooyard.Rpc.NettyImpl.Support
{

    /// <summary>
    /// The type Rpc remoting server.
    /// 
    /// </summary>
    public abstract class AbstractNettyRemotingServer : AbstractNettyRemoting, IRemotingServer
	{
		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(AbstractNettyRemotingServer));

		private readonly NettyServerBootstrap serverBootstrap;
		public readonly object @lock = new();

		public override async Task Init()
		{
			await base.Init();
			await serverBootstrap.Start();
		}

		/// <summary>
		/// Instantiates a new Rpc remoting server.
		/// </summary>
		/// <param name="messageExecutor">   the message executor </param>
		/// <param name="nettyServerConfig"> the netty server config </param>
		public AbstractNettyRemotingServer(MultithreadEventLoopGroup messageExecutor, NettyServerConfig nettyServerConfig)
		//public AbstractNettyRemotingServer(ThreadPoolExecutor messageExecutor, NettyServerConfig nettyServerConfig)
			: base(messageExecutor)
		{
			serverBootstrap = new NettyServerBootstrap(nettyServerConfig);
			serverBootstrap.SetChannelHandlers(new ServerHandler(this));
		}

		public virtual async Task<object> SendSyncRequest(string resourceId, string clientId, object msg)
		{
			IChannel channel = ChannelManager.GetChannel(resourceId, clientId);
			if (channel == null)
			{
				throw new Exception($"rm client is not connected. dbkey:{resourceId},clientId:{clientId}");
			}
			RpcMessage rpcMessage = BuildRequestMessage(msg, ProtocolConstants.MSGTYPE_RESQUEST_SYNC);
			return await base.SendSync(channel, rpcMessage, NettyServerConfig.RpcRequestTimeout);
		}

		public virtual async Task<object> SendSyncRequest(IChannel channel, object msg)
		{
			if (channel == null)
			{
				throw new Exception("client is not connected");
			}
			RpcMessage rpcMessage = BuildRequestMessage(msg, ProtocolConstants.MSGTYPE_RESQUEST_SYNC);
			return await base.SendSync(channel, rpcMessage, NettyServerConfig.RpcRequestTimeout);
		}

		public virtual async Task SendAsyncRequest(IChannel channel, object msg)
		{
			if (channel == null)
			{
				throw new Exception("client is not connected");
			}
			RpcMessage rpcMessage = BuildRequestMessage(msg, ProtocolConstants.MSGTYPE_RESQUEST_ONEWAY);
			await base.SendAsync(channel, rpcMessage);
		}

		public virtual async Task SendAsyncResponse(RpcMessage rpcMessage, IChannel channel, object msg)
		{
			IChannel clientChannel = channel;
			if (!(msg is HeartbeatMessage))
			{
				clientChannel = ChannelManager.GetSameClientChannel(channel);
			}
			if (clientChannel != null)
			{
				RpcMessage rpcMsg = BuildResponseMessage(rpcMessage, msg, msg is HeartbeatMessage ? ProtocolConstants.MSGTYPE_HEARTBEAT_RESPONSE : ProtocolConstants.MSGTYPE_RESPONSE);
				await base.SendAsync(clientChannel, rpcMsg);
			}
			else
			{
				throw new Exception("channel is error.");
			}
		}

		public virtual async Task RegisterProcessor(int messageType, IRemotingProcessor processor, IExecutorService executor)
		{
			Pair<IRemotingProcessor, IExecutorService> pair = new (processor, executor);
			this.processorTable[messageType] = pair;
			await Task.CompletedTask;
		}

		/// <summary>
		/// Sets listen port.
		/// </summary>
		/// <param name="listenPort"> the listen port </param>
		public virtual int ListenPort
		{
			set
			{
				serverBootstrap.ListenPort = value;
			}
			get
			{
				return serverBootstrap.ListenPort;
			}
		}

        public override async ValueTask DisposeAsync()
        {
			await serverBootstrap.Shutdown();
			await base.DisposeAsync();
        }

		/// <summary>
		/// Debug log.
		/// </summary>
		/// <param name="format"> the info </param>
		/// <param name="arguments"> the arguments </param>
		protected internal virtual void DebugLog(string format, params object[] arguments)
		{
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogDebug(string.Format(format, arguments));
			}
		}

		private async Task CloseChannelHandlerContext(IChannelHandlerContext ctx)
		{
			if (Logger().IsEnabled(LogLevel.Information))
			{
				Logger().LogInformation($"closeChannelHandlerContext channel:{ctx.Channel}");
			}
			await ctx.DisconnectAsync();
			await ctx.CloseAsync();
		}

		/// <summary>
		/// The type ServerHandler.
		/// </summary>
		internal class ServerHandler : ChannelDuplexHandler
		{
			public override bool IsSharable => true;

			private readonly AbstractNettyRemotingServer outerInstance;
			public ServerHandler(AbstractNettyRemotingServer outerInstance)
			{
				this.outerInstance = outerInstance;
			}
			/// <summary>
			/// Channel read.
			/// </summary>
			/// <param name="ctx"> the ctx </param>
			/// <param name="msg"> the msg </param>
			/// <exception cref="Exception"> the exception </exception>
			public override void ChannelRead(IChannelHandlerContext ctx, object msg)
			{
				if (!(msg is RpcMessage))
				{
					return;
				}
				outerInstance.ProcessMessage(ctx, (RpcMessage)msg);
			}

			public override void ChannelWritabilityChanged(IChannelHandlerContext ctx)
			{
				lock (outerInstance.@lock)
				{
					if (ctx.Channel.IsWritable)
					{
						Monitor.PulseAll(outerInstance.@lock);
					}
				}
				ctx.FireChannelWritabilityChanged();
			}

			/// <summary>
			/// Channel inactive.
			/// </summary>
			/// <param name="ctx"> the ctx </param>
			/// <exception cref="Exception"> the exception </exception>
			public override void ChannelInactive(IChannelHandlerContext ctx)
			{
				outerInstance.DebugLog($"inactive:{ctx}");
				if (outerInstance.messageExecutor.IsShutdown)
				{
					return;
				}
				HndleDisconnect(ctx);
				base.ChannelInactive(ctx);
			}

			internal virtual void HndleDisconnect(IChannelHandlerContext ctx)
			{
				string ipAndPort = NetUtil.ToStringAddress(ctx.Channel.RemoteAddress);
				RpcContext rpcContext = ChannelManager.GetContextFromIdentified(ctx.Channel);
				if (Logger().IsEnabled(LogLevel.Information))
				{
					Logger().LogInformation($"{ipAndPort} to server channel inactive.");
				}
				if (rpcContext != null && rpcContext.ClientRole != null)
				{
					rpcContext.Release();
					if (Logger().IsEnabled(LogLevel.Information))
					{
						Logger().LogInformation($"remove channel:{ctx.Channel} context:{rpcContext}");
					}
				}
				else
				{
					if (Logger().IsEnabled(LogLevel.Information))
					{
						Logger().LogInformation("remove unused channel:" + ctx.Channel);
					}
				}
			}

			/// <summary>
			/// Exception caught.
			/// </summary>
			/// <param name="ctx">   the ctx </param>
			/// <param name="cause"> the cause </param>
			/// <exception cref="Exception"> the exception </exception>
			public override void ExceptionCaught(IChannelHandlerContext ctx, Exception cause)
			{
				try
				{
					if (cause is DecoderException && ChannelManager.GetContextFromIdentified(ctx.Channel) == null)
					{
						return;
					}
					Logger().LogError(cause, $"exceptionCaught:{cause.Message}, channel:{ctx.Channel}");
					base.ExceptionCaught(ctx, cause);
				}
				finally
				{
					ChannelManager.ReleaseRpcContext(ctx.Channel);
				}
			}

			/// <summary>
			/// User event triggered.
			/// </summary>
			/// <param name="ctx"> the ctx </param>
			/// <param name="evt"> the evt </param>
			/// <exception cref="Exception"> the exception </exception>
			public override void UserEventTriggered(IChannelHandlerContext ctx, object evt)
			{
				if (evt is IdleStateEvent idleStateEvent)
				{
					outerInstance.DebugLog($"idle:{evt}");
					if (idleStateEvent.State == IdleState.ReaderIdle)
					{
						if (Logger().IsEnabled(LogLevel.Information))
						{
							Logger().LogInformation($"channel:{ctx.Channel} read idle.");
						}
						HndleDisconnect(ctx);
						try
						{
							outerInstance.CloseChannelHandlerContext(ctx).GetAwaiter().GetResult();
						}
						catch (Exception e)
						{
							Logger().LogError(e, e.Message);
						}
					}
				}
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
}