using DotNetty.Common.Concurrency;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Zooyard.Atomic;
using Zooyard.Rpc.NettyImpl.Support.V1;
using Zooyard.Exceptions;
using Zooyard.Logging;

namespace Zooyard.Rpc.NettyImpl.Support
{
    public class NettyClientBootstrap: IRemotingBootstrap
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyClientBootstrap));

        private readonly NettyClientConfig _nettyClientConfig;
        private readonly Bootstrap _bootstrap = new ();
        private readonly IEventLoopGroup _eventLoopGroupWorker;
        private IEventExecutorGroup _defaultEventExecutorGroup;
        private readonly AtomicBoolean _initialized = new (false);
        //private const string THREAD_PREFIX_SPLIT_CHAR = "_";
        private readonly NettyPoolKey.TransactionRole _transactionRole;
        private IChannelHandler[] channelHandlers;


        public NettyClientBootstrap(NettyClientConfig nettyClientConfig, 
            IEventExecutorGroup eventExecutorGroup, 
            NettyPoolKey.TransactionRole transactionRole)
        {
            if (nettyClientConfig == null)
            {
                nettyClientConfig = new NettyClientConfig();
                if (Logger().IsEnabled(LogLevel.Information)) 
                {
                    Logger().LogInformation("use default netty client config.");
                }
            }
            _nettyClientConfig = nettyClientConfig;
            //int selectorThreadSizeThreadSize = _nettyClientConfig.ClientSelectorThreadSize;
            _transactionRole = transactionRole;
            //this.eventLoopGroupWorker = new NioEventLoopGroup(selectorThreadSizeThreadSize, new NamedThreadFactory(getThreadPrefix(this.nettyClientConfig.ClientSelectorThreadPrefix), selectorThreadSizeThreadSize));
            _eventLoopGroupWorker = new MultithreadEventLoopGroup(_nettyClientConfig.ClientSelectorThreadSize);
            _defaultEventExecutorGroup = eventExecutorGroup;
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
        /// Sets channel handlers.
        /// </summary>
        /// <param name="ChannelHandler"></param>
        /// <param name=""></param>
        internal void SetChannelHandlers(params IChannelHandler[] handlers)
        {
            if (null != handlers)
            {
                channelHandlers = handlers;
            }
        }

        public virtual async Task Start()
        {
            if (this._defaultEventExecutorGroup == null)
            {
                this._defaultEventExecutorGroup = new MultithreadEventLoopGroup(_nettyClientConfig.ClientWorkerThreads);
                //this.defaultEventExecutorGroup = new DefaultEventExecutorGroup(nettyClientConfig.ClientWorkerThreads, new NamedThreadFactory(getThreadPrefix(nettyClientConfig.ClientWorkerThreadPrefix), nettyClientConfig.ClientWorkerThreads));
            }
            //this.bootstrap.group(this.eventLoopGroupWorker).channel(nettyClientConfig.ClientChannelClazz).option(ChannelOption.TCP_NODELAY, true).option(ChannelOption.SO_KEEPALIVE, true).option(ChannelOption.CONNECT_TIMEOUT_MILLIS, nettyClientConfig.ConnectTimeoutMillis).option(ChannelOption.SO_SNDBUF, nettyClientConfig.ClientSocketSndBufSize).option(ChannelOption.SO_RCVBUF, nettyClientConfig.ClientSocketRcvBufSize);
            _bootstrap.Group(_eventLoopGroupWorker)
                .ChannelFactory(()=> 
                {
                    IChannel result = _nettyClientConfig.ClientChannelClazz.GetConstructor(Array.Empty<Type>())
                    .Invoke(Array.Empty<object>()) as IChannel;
                    return result;
                })
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.SoKeepalive, true)
                .Option(ChannelOption.ConnectTimeout, TimeSpan.FromMilliseconds(_nettyClientConfig.ConnectTimeoutMillis) )
                .Option(ChannelOption.SoSndbuf, _nettyClientConfig.ClientSocketSndBufSize)
                .Option(ChannelOption.SoRcvbuf, _nettyClientConfig.ClientSocketRcvBufSize);

            if (_nettyClientConfig.enableNative())
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (Logger().IsEnabled(LogLevel.Information))
                    {
                        Logger().LogInformation("client run on macOS");
                    }
                }
                //else
                //{
                //   _bootstrap.Option(EpollChannelOption.EPOLL_MODE, EpollMode.EDGE_TRIGGERED).Option(EpollChannelOption.TCP_QUICKACK, true);
                //}
            }

            _bootstrap.Handler(new ChannelInitializerAnonymousInnerClass(this));

            if (_initialized.CompareAndSet(false, true))
            {
                Logger().LogInformation("NettyClientBootstrap has started");
            }

            await Task.CompletedTask;
        }

        private class ChannelInitializerAnonymousInnerClass : ChannelInitializer<IChannel>
        {
            public override bool IsSharable => true;

            private readonly NettyClientBootstrap outerInstance;

            public ChannelInitializerAnonymousInnerClass(NettyClientBootstrap outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override void InitChannel(IChannel channel)
            {
                channel.Pipeline.AddLast(new IdleStateHandler(outerInstance._nettyClientConfig.ChannelMaxReadIdleSeconds,
               outerInstance._nettyClientConfig.ChannelMaxWriteIdleSeconds,
               outerInstance._nettyClientConfig.ChannelMaxAllIdleSeconds))
               .AddLast(new ProtocolV1Decoder())
               .AddLast(new ProtocolV1Encoder());

                if (outerInstance.channelHandlers != null)
                {
                    outerInstance.AddChannelPipelineLast(channel, outerInstance.channelHandlers);
                }
            }
        }

        public virtual async Task Shutdown()
        {
            try
            {
                await _eventLoopGroupWorker?.ShutdownGracefullyAsync();
                await _defaultEventExecutorGroup?.ShutdownGracefullyAsync();
            }
            catch (Exception exx)
            {
                Logger().LogError(exx, $"Failed to shutdown: {exx.Message}");
            }
        }

        /// <summary>
        /// Gets new channel.
        /// </summary>
        /// <param name="address"> the address </param>
        /// <returns> the new channel </returns>
        public virtual async Task<IChannel> GetNewChannel(IPEndPoint address)
        {
            //var channel = await _bootstrap.ConnectAsync(address);
            IChannel channel;
            var f = _bootstrap.ConnectAsync(address);

            try
            {
                Task.WaitAll(new[] { f }, this._nettyClientConfig.ConnectTimeoutMillis);
                if (f.IsCanceled)
                {
                    throw new FrameworkException(f.Exception, "connect cancelled, can not connect to services-server.");
                }
                else if (!f.IsCompleted)
                {
                    throw new FrameworkException(f.Exception, "connect failed, can not connect to services-server.");
                }
                else
                {
                    channel = await f;
                }
            }
            catch (Exception e)
            {
                throw new FrameworkException(e, "can not connect to services-server.");
            }
            return channel;
        }

        ///// <summary>
        ///// Gets thread prefix.
        ///// </summary>
        ///// <param name="threadPrefix"> the thread prefix </param>
        ///// <returns> the thread prefix </returns>
        //private string GetThreadPrefix(string threadPrefix)
        //{
        //    return threadPrefix + THREAD_PREFIX_SPLIT_CHAR + _transactionRole;
        //}
    }
}
