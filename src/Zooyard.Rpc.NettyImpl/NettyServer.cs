using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyServer : AbstractServer
    {
        //UseLibuv

        public IEventLoopGroup TheBossGroup { get; set; }
        public IEventLoopGroup TheWorkerGroup { get; set; }
        public IServerChannel TheServerChannel { get; set; }
        public IList<IChannelHandler> ChannelHandlers { get; set; }
        public int ThePort { get; set; }



        public bool IsSsl { get; set; } = false;
        public string Pfx { get; set; } = "dotnetty.com";
        public string Pwd { get; set; } = "password";

        private IChannel _channel;

        public override void DoExport()
        {

            
            //if (_logger.IsEnabled(LogLevel.Debug))
            //    _logger.LogDebug($"准备启动服务主机，监听地址：{TheEndPoint}。");
            Console.WriteLine($"准备启动服务主机，监听地址：{ThePort}。");
            
            X509Certificate2 tlsCertificate = null;
            if (IsSsl)
            {
                tlsCertificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Pfx}.pfx"), Pwd);
            }

            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(TheBossGroup, TheWorkerGroup)
                .ChannelFactory(()=> TheServerChannel)
                .Option(ChannelOption.SoBacklog, 100)
                .Handler(new LoggingHandler("SRV-LSTN"))
                .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;

                    if (tlsCertificate != null)
                    {
                        pipeline.AddLast("tls", TlsHandler.Server(tlsCertificate));
                    }

                    //pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                    //pipeline.AddLast(new LengthFieldPrepender(4));
                    //pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
                    //pipeline.AddLast(new ServerHandler(TheService));

                    pipeline.AddLast(ChannelHandlers?.ToArray());
                    
                }));
            try
            {
                
                _channel = bootstrap.BindAsync(ThePort).GetAwaiter().GetResult();
                //if (_logger.IsEnabled(LogLevel.Debug))
                //    _logger.LogDebug($"服务主机启动成功，监听地址：{endPoint}。");
                Console.WriteLine($"服务主机启动成功，监听地址：{ThePort}。");
            }
            catch
            {
                //_logger.LogError($"服务主机启动失败，监听地址：{endPoint}。 ");
                Console.WriteLine($"服务主机启动失败，监听地址：{ThePort}。");
            }

        }

        public override void DoDispose()
        {
            //向注册中心发送注销请求

            Task.Run(async () =>
            {
                await _channel.CloseAsync();
                await _channel.DisconnectAsync();
                await TheBossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
                await TheWorkerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));

            }).Wait();

        }

        
    }

    public class ServerHandler : ChannelHandlerAdapter
    {
        readonly object _service;
        public ServerHandler(object service)
        {
            _service = service;
        }

        #region Overrides of ChannelHandlerAdapter

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            //接收到消息
            //调用服务器端接口
            //将返回值编码
            //将返回值发送到客户端
            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                var bytes = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(bytes);
                var transportMessage = bytes.Desrialize<TransportMessage>();
                var rpc = transportMessage.GetContent<RemoteInvokeMessage>();
                //var rpcJson = buffer.ToString(Encoding.UTF8);
                //Console.WriteLine($"Received from client: {rpcJson}");
                //var transportMessage = JsonConvert.DeserializeObject<TransportMessage>(rpcJson);
                //var rpc = JsonConvert.DeserializeObject<RemoteInvokeMessage>(transportMessage.Content.ToString());
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
                    //socket.Send(sendByte, sendByte.Length, 0);
                }
                catch (Exception ex)
                {
                    remoteInvoker.ExceptionMessage = ex.Message;
                    remoteInvoker.StatusCode = 500;
                }
                var resultData = TransportMessage.CreateInvokeResultMessage(transportMessage.Id, remoteInvoker);
                var sendByte = resultData.Serialize();
                //var sendStr = JsonConvert.SerializeObject(resultData);
                //var sendByte = Encoding.ASCII.GetBytes(sendStr);
                var sendBuffer = Unpooled.WrappedBuffer(sendByte);
                context.WriteAsync(sendBuffer);
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
            Console.WriteLine($"与服务器：{context.Channel.RemoteAddress}通信时发送了错误。");
            //if (_logger.IsEnabled(LogLevel.Error))
            //    _logger.LogError(exception, $"与服务器：{context.Channel.RemoteAddress}通信时发送了错误。");
        }

        #endregion Overrides of ChannelHandlerAdapter
    }

    //public class RpcData
    //{
    //    public string Method { get; set; }
    //    public object[] Arguments { get; set; }
    //}

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
        ///// <summary>
        ///// 服务Id。
        ///// </summary>
        //public string ServiceId { get; set; }

        //public bool DecodeJOject { get; set; }

        //public string ServiceKey { get; set; }
        ///// <summary>
        ///// 服务参数。
        ///// </summary>
        //public IDictionary<string, object> Parameters { get; set; }

        //public IDictionary<string, object> Attachments { get; set; }
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
