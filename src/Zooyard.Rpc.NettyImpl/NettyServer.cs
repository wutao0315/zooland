using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyServer : AbstractServer
    {
        //UseLibuv

        private readonly IEventLoopGroup _bossGroup;
        private readonly IEventLoopGroup _workerGroup;
        private readonly IServerChannel _serverChannel;
        private readonly IEnumerable<IChannelHandler> _channelHandlers;
        private readonly object _service;
        private readonly int _port;
        private readonly bool _isSsl = false;
        private readonly string _pfx = "dotnetty.com";
        private readonly string _pwd = "password";

        private readonly ILogger _logger;
        public NettyServer(IEventLoopGroup bossGroup, 
            IEventLoopGroup workerGroup, 
            IServerChannel serverChannel,
            IEnumerable<IChannelHandler> channelHandlers,
            object service,
            int port,
            bool isSsl,
            string pfx,
            string pwd,
            ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<NettyServer>();
            _bossGroup = bossGroup;
            _workerGroup = workerGroup;
            _serverChannel = serverChannel;
            _channelHandlers = channelHandlers;
            _service = service;
            _port = port;
            _isSsl = isSsl;
            _pfx = pfx;
            _pwd = pwd;
        }
        private IChannel _channel;

        public override void DoExport()
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"ready to start the server on port:{_port}.");
            }
                
            
            X509Certificate2 tlsCertificate = null;
            if (_isSsl)
            {
                tlsCertificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{_pfx}.pfx"), _pwd);
            }

            var bootstrap = new ServerBootstrap();
            bootstrap
                .ChannelFactory(()=> _serverChannel)
                //.Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr)
                //.Option(ChannelOption.SoKeepalive, Settings.TcpKeepAlive)
                //.Option(ChannelOption.TcpNodelay, Settings.TcpNoDelay)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.SoBacklog, 100)
                .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .Group(_bossGroup, _workerGroup)
                .Handler(new DotNetty.Handlers.Logging.LoggingHandler("SRV-LSTN"))
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;

                    if (tlsCertificate != null)
                    {
                        pipeline.AddLast("tls", TlsHandler.Server(tlsCertificate));
                    }

                    //pipeline.AddLast(new DotNetty.Handlers.Logging.LoggingHandler("SRV-CONN"));
                    //pipeline.AddLast(new LengthFieldPrepender(4));
                    //pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));

                    pipeline.AddLast(_channelHandlers?.ToArray());
                    pipeline.AddLast(new ServerHandler(_service, _logger));

                }));

            //if (Settings.ReceiveBufferSize.HasValue) bootstrap.Option(ChannelOption.SoRcvbuf, Settings.ReceiveBufferSize.Value);
            //if (Settings.SendBufferSize.HasValue) bootstrap.Option(ChannelOption.SoSndbuf, Settings.SendBufferSize.Value);
            //if (Settings.WriteBufferHighWaterMark.HasValue) bootstrap.Option(ChannelOption.WriteBufferHighWaterMark, Settings.WriteBufferHighWaterMark.Value);
            //if (Settings.WriteBufferLowWaterMark.HasValue) bootstrap.Option(ChannelOption.WriteBufferLowWaterMark, Settings.WriteBufferLowWaterMark.Value);

            try
            {
                _channel = bootstrap.BindAsync(IPAddress.Any,_port).GetAwaiter().GetResult();

                _logger.LogInformation($"Started the netty server ...");
                Console.WriteLine($"Started the netty server ...");
               
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"server start fail on port：{_port}。 ");
            }

        }

        public override void DoDispose()
        {

            Task.Run(async () =>
            {
                await _channel.CloseAsync();
                await _channel.DisconnectAsync();
                await _bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
                await _workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));

            }).Wait();

        }

        
    }

    internal class ServerHandler : ChannelHandlerAdapter
    {
        private readonly object _service;
        private readonly ILogger _logger;

        public ServerHandler(object service, ILogger logger)
        {
            _service = service;
            _logger = logger;
        }

        #region Overrides of ChannelHandlerAdapter

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
                    _logger.LogError(ex, ex.Message);
                }
                var resultData = TransportMessage.CreateInvokeResultMessage(transportMessage.Id, remoteInvoker);
                var sendByte = resultData.Serialize();
                var sendBuffer = Unpooled.WrappedBuffer(sendByte);
                context.WriteAndFlushAsync(sendBuffer).GetAwaiter().GetResult();
            }

        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            //客户端主动断开需要应答，否则socket变成CLOSE_WAIT状态导致socket资源耗尽
            context.CloseAsync();
            Console.WriteLine($"server error on transport with {context.Channel.RemoteAddress}:{exception.Message}");
            _logger.LogError(exception, $"server error on transport with {context.Channel.RemoteAddress}:{exception.Message}");
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
