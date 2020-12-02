using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyServer : AbstractServer
    {
        //UseLibuv
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyServer));

        private readonly object _service;
        private readonly bool _isSsl = false;
        private readonly string _pfx = "dotnetty.com";
        private readonly string _pwd = "password";

        private readonly IEventLoopGroup _serverEventLoopGroup;
        protected internal volatile IChannel ServerChannel;
        internal readonly ConcurrentSet<IChannel> ConnectionGroup;

        public NettyServer(URL config,
            object service,
            bool isSsl,
            string pfx,
            string pwd,
            IRegistryService registryService)
            : base(registryService)
        {
            Settings = NettyTransportSettings.Create(config);
            ConnectionGroup = new ConcurrentSet<IChannel>();

            _serverEventLoopGroup = new MultithreadEventLoopGroup(Settings.ServerSocketWorkerPoolSize);
            
            _service = service;
            _isSsl = isSsl;
            _pfx = pfx;
            _pwd = pwd;
        }
        internal NettyTransportSettings Settings { get; }

       
        private ServerBootstrap ServerFactory()
        {
            X509Certificate2 tlsCertificate = null;
            if (_isSsl)
            {
                tlsCertificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{_pfx}.pfx"), _pwd);
            }

            var addressFamily = Settings.DnsUseIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            var server = new ServerBootstrap()
                .Group(_serverEventLoopGroup)
                .Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr)
                .Option(ChannelOption.SoKeepalive, Settings.TcpKeepAlive)
                .Option(ChannelOption.TcpNodelay, Settings.TcpNoDelay)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.SoBacklog, Settings.Backlog)
                .Option(ChannelOption.Allocator, Settings.EnableBufferPooling ? (IByteBufferAllocator)PooledByteBufferAllocator.Default : UnpooledByteBufferAllocator.Default)
                .ChannelFactory(() => Settings.EnforceIpFamily
                    ? new TcpServerSocketChannel(addressFamily)
                    : new TcpServerSocketChannel())
                .ChildHandler(new ActionChannelInitializer<TcpSocketChannel>((channel =>
                {
                    var pipeline = channel.Pipeline;

                    if (tlsCertificate != null)
                    {
                        pipeline.AddLast("tls", TlsHandler.Server(tlsCertificate));
                    }

                    //pipeline.AddLast(new DotNetty.Handlers.Logging.LoggingHandler("SRV-CONN"));
                    //pipeline.AddLast(new LengthFieldPrepender(4));
                    //pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
                    pipeline.AddLast(new NettyLoggingHandler());
                    SetInitialChannelPipeline(channel);
                    //pipeline.AddLast(_channelHandlers?.ToArray());
                    pipeline.AddLast(new ServerHandler(this, _service));

                })));

            if (Settings.ReceiveBufferSize.HasValue) server.Option(ChannelOption.SoRcvbuf, Settings.ReceiveBufferSize.Value);
            if (Settings.SendBufferSize.HasValue) server.Option(ChannelOption.SoSndbuf, Settings.SendBufferSize.Value);
            if (Settings.WriteBufferHighWaterMark.HasValue) server.Option(ChannelOption.WriteBufferHighWaterMark, Settings.WriteBufferHighWaterMark.Value);
            if (Settings.WriteBufferLowWaterMark.HasValue) server.Option(ChannelOption.WriteBufferLowWaterMark, Settings.WriteBufferLowWaterMark.Value);

            return server;
        }

        private void SetInitialChannelPipeline(IChannel channel)
        {
            var pipeline = channel.Pipeline;

            if (Settings.LogTransport)
            {
                pipeline.AddLast("Logger", new NettyLoggingHandler());
            }

            pipeline.AddLast("FrameDecoder", new LengthFieldBasedFrameDecoder(Settings.ByteOrder, Settings.MaxFrameSize, 0, 4, 0, 4, true));
            if (Settings.BackwardsCompatibilityModeEnabled)
            {
                pipeline.AddLast("FrameEncoder", new HeliosBackwardsCompatabilityLengthFramePrepender(4, false));
            }
            else
            {
                pipeline.AddLast("FrameEncoder", new LengthFieldPrepender(Settings.ByteOrder, 4, 0, false));
            }
        }
        public override async Task DoExport()
        {
            Logger().LogDebug($"ready to start the server on port:{Settings.Port}.");

            var newServerChannel = await ServerFactory().BindAsync(IPAddress.Any, Settings.Port);

            // Block reads until a handler actor is registered
            // no incoming connections will be accepted until this value is reset
            // it's possible that the first incoming association might come in though

            //newServerChannel.Configuration.AutoRead = false;
            ConnectionGroup.TryAdd(newServerChannel);
            ServerChannel = newServerChannel;

            Logger().LogInformation($"Started the netty server ...");
            Console.WriteLine($"Started the netty server ...");
            
        }

        public override async Task DoDispose()
        {
            try
            {
                foreach (var channel in ConnectionGroup)
                {
                    await channel.CloseAsync();
                }
                await ServerChannel?.CloseAsync();

                //var tasks = new List<Task>();
                //foreach (var channel in ConnectionGroup)
                //{
                //    tasks.Add(channel.CloseAsync());
                //}
                //var all = Task.WhenAll(tasks);
                //all.ConfigureAwait(false).GetAwaiter().GetResult();

                //var server = ServerChannel?.CloseAsync() ?? Task.CompletedTask;
                //server.ConfigureAwait(false).GetAwaiter().GetResult();

            }
            finally
            {
                // free all of the connection objects we were holding onto
                ConnectionGroup.Clear();
                // shutting down the worker groups can take up to 10 seconds each. Let that happen asnychronously.
               await _serverEventLoopGroup.ShutdownGracefullyAsync();
            }

        }

        
    }
    internal abstract class ServerCommonHandlers : ChannelHandlerAdapter
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ServerCommonHandlers));
        protected readonly NettyServer Server;
        

        protected ServerCommonHandlers(NettyServer server)
        {
            Server = server;
        }


        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            if (!Server.ConnectionGroup.TryAdd(context.Channel))
            {
                Logger().LogWarning($"Unable to ADD channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id}) to connection group. May not shut down cleanly.");
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            if (!Server.ConnectionGroup.TryRemove(context.Channel))
            {
                Logger().LogWarning($"Unable to REMOVE channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={ context.Channel.Id}) from connection group. May not shut down cleanly.");
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);
            Logger().LogError(exception, $"Error caught channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id})");
        }

    }
    internal class ServerHandler : ServerCommonHandlers
    {
        private readonly object _service;
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ServerHandler));

        public ServerHandler(NettyServer server, object service):base(server)
        {
            _service = service;
        }

        #region Overrides of ChannelHandlerAdapter
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
        }
        
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            //接收到消息
            //调用服务器端接口
            //将返回值编码
            //将返回值发送到客户端
            if (message is IByteBuffer buffer)
            {
                var bytes = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(bytes);
                var transportMessage = bytes.Desrialize<TransportMessage>();
                context.FireChannelRead(transportMessage);
                ReferenceCountUtil.SafeRelease(buffer);

                var rpc = transportMessage.GetContent<RemoteInvokeMessage>();

                var methodName = rpc.Method;
                var arguments = rpc.Arguments;
                var types = (from item in arguments select item.GetType()).ToArray();
                var remoteInvoker = new RemoteInvokeResultMessage
                {
                    ExceptionMessage = "",
                    StatusCode = 200
                };
                try
                {
                    var method = _service.GetType().GetMethod(methodName, types);
                    var result = method.Invoke(_service, arguments);
                    remoteInvoker.Result = result;
                }
                catch (Exception ex)
                {
                    remoteInvoker.ExceptionMessage = ex.Message;
                    remoteInvoker.StatusCode = 500;
                    Logger().LogError(ex, ex.Message);
                }
                var resultData = TransportMessage.CreateInvokeResultMessage(transportMessage.Id, remoteInvoker);
                var sendByte = resultData.Serialize();
                var sendBuffer = Unpooled.WrappedBuffer(sendByte);
                context.WriteAndFlushAsync(sendBuffer).GetAwaiter().GetResult();
            }
            ReferenceCountUtil.SafeRelease(message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="context">TBD</param>
        /// <param name="exception">TBD</param>
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            var se = exception as SocketException;

            if (se?.SocketErrorCode == SocketError.OperationAborted)
            {
                Logger().LogInformation($"Socket read operation aborted. Connection is about to be closed. Channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id})");
            }
            else if (se?.SocketErrorCode == SocketError.ConnectionReset)
            {
                Logger().LogInformation($"Connection was reset by the remote peer. Channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id})");
            }
            else
            {
                base.ExceptionCaught(context, exception);
            }

            Console.WriteLine($"Exception: {exception.Message}");
            Logger().LogError(exception, exception.Message);
            context.CloseAsync();
        }


        #endregion Overrides of ChannelHandlerAdapter
    }

    /// <summary>
    /// 传输消息模型。
    /// </summary>
    [Serializable]
    public class TransportMessage
    {

        public TransportMessage()
        {
        }

        public TransportMessage(object content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            ContentType = content.GetType().FullName;
        }

        public TransportMessage(object content, string fullName)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            ContentType = fullName;
        }

        /// <summary>
        /// 消息Id。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 消息内容。
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// 内容类型。
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 是否调用消息。
        /// </summary>
        /// <returns>如果是则返回true，否则返回false。</returns>
        public bool IsInvokeMessage()
        {
            return ContentType == typeof(RemoteInvokeMessage).FullName;
        }

        /// <summary>
        /// 是否是调用结果消息。
        /// </summary>
        /// <returns>如果是则返回true，否则返回false。</returns>
        public bool IsInvokeResultMessage()
        {
            return ContentType == typeof(RemoteInvokeResultMessage).FullName;
        }



        /// <summary>
        /// 获取内容。
        /// </summary>
        /// <typeparam name="T">内容类型。</typeparam>
        /// <returns>内容实例。</returns>
        public T GetContent<T>()
        {
            return (T)Content;
        }

        /// <summary>
        /// 创建一个调用传输消息。
        /// </summary>
        /// <param name="invokeMessage">调用实例。</param>
        /// <returns>调用传输消息。</returns>
        public static TransportMessage CreateInvokeMessage(RemoteInvokeMessage invokeMessage)
        {
            return new TransportMessage(invokeMessage, typeof(RemoteInvokeMessage).FullName)
            {
                Id = Guid.NewGuid().ToString("N")
            };
        }

        /// <summary>
        /// 创建一个调用结果传输消息。
        /// </summary>
        /// <param name="id">消息Id。</param>
        /// <param name="invokeResultMessage">调用结果实例。</param>
        /// <returns>调用结果传输消息。</returns>
        public static TransportMessage CreateInvokeResultMessage(string id, RemoteInvokeResultMessage invokeResultMessage)
        {
            return new TransportMessage(invokeResultMessage, typeof(RemoteInvokeResultMessage).FullName)
            {
                Id = id
            };
        }
    }
    /// <summary>
    /// 远程调用结果消息。
    /// </summary>
    [Serializable]
    public class RemoteInvokeResultMessage
    {
        /// <summary>
        /// 异常消息。
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// 状态码
        /// </summary>
        public int StatusCode { get; set; } = 200;
        /// <summary>
        /// 结果内容。
        /// </summary>
        public object Result { get; set; }
    }
    /// <summary>
    /// 远程调用消息。
    /// </summary>
    [Serializable]
    public class RemoteInvokeMessage
    {
        public string Method { get; set; }
        public object[] Arguments { get; set; }
    }

    /// <summary>
    /// 接受到消息的委托。
    /// </summary>
    /// <param name="sender">消息发送者。</param>
    /// <param name="message">接收到的消息。</param>
    public delegate Task ReceivedDelegate(TransportMessage message);
    /// <summary>
    /// 一个抽象的消息监听者。
    /// </summary>
    public interface IMessageListener
    {
        /// <summary>
        /// 接收到消息的事件。
        /// </summary>
        event ReceivedDelegate Received;

        /// <summary>
        /// 触发接收到消息事件。
        /// </summary>
        /// <param name="sender">消息发送者。</param>
        /// <param name="message">接收到的消息。</param>
        /// <returns>一个任务。</returns>
        Task OnReceived(TransportMessage message);
    }

    /// <summary>
    /// 消息监听者。
    /// </summary>
    public class MessageListener : IMessageListener
    {
        #region Implementation of IMessageListener

        /// <summary>
        /// 接收到消息的事件。
        /// </summary>
        public event ReceivedDelegate Received;

        /// <summary>
        /// 触发接收到消息事件。
        /// </summary>
        /// <param name="sender">消息发送者。</param>
        /// <param name="message">接收到的消息。</param>
        /// <returns>一个任务。</returns>
        public async Task OnReceived(TransportMessage message)
        {
            if (Received == null)
                return;
            await Received(message);
        }

        #endregion Implementation of IMessageListener
    }
}
