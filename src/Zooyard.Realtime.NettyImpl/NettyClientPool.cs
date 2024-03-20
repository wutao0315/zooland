using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using Zooyard.DotNettyImpl.Adapter;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.DotNettyImpl;

public class NettyClientPool : AbstractClientPool
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ITransportMessageEncoder _transportMessageEncoder;
    private readonly ITransportMessageDecoder _transportMessageDecoder;
    private readonly ConcurrentDictionary<EndPoint, Lazy<Task<ITransportClient>>> _clients = new ();

    private static readonly AttributeKey<IMessageSender> messageSenderKey = AttributeKey<IMessageSender>.ValueOf(typeof(NettyClientPool), nameof(IMessageSender));
    private static readonly AttributeKey<IMessageListener> messageListenerKey = AttributeKey<IMessageListener>.ValueOf(typeof(NettyClientPool), nameof(IMessageListener));
    private static readonly AttributeKey<EndPoint> origEndPointKey = AttributeKey<EndPoint>.ValueOf(typeof(NettyClientPool), nameof(EndPoint));

    public const string TIMEOUT_KEY = "http_timeout";
    public const int DEFAULT_TIMEOUT = 5000;

    public NettyClientPool(
        ILoggerFactory loggerFactory,
        ITransportMessageCodecFactory transportMessageCodecFactory):base(loggerFactory.CreateLogger<NettyClientPool>())
    {
        _transportMessageEncoder = transportMessageCodecFactory.GetEncoder();
        _transportMessageDecoder = transportMessageCodecFactory.GetDecoder();
        _loggerFactory = loggerFactory;
    }

    protected override async Task<IClient> CreateClient(URL url)
    {
        var isDns = url.GetParameter("dns", false);
        EndPoint key = isDns ? new DnsEndPoint(url.Host, url.Port) : new IPEndPoint(IPAddress.Parse(url.Host), url.Port);
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug($"准备为服务端地址：{key}创建客户端。");
        try
        {
            var transportClient = await _clients.GetOrAdd(key
                , k => new Lazy<Task<ITransportClient>>(async () =>
                {
                    //客户端对象
                    var bootstrap = GetBootstrap(url);
                    //异步连接返回channel
                    var channel = await bootstrap.ConnectAsync(k);
                    var messageListener = new MessageListener();
                    //设置监听
                    channel.GetAttribute(messageListenerKey).Set(messageListener);

                    //实例化发送者
                    var messageSender = new DotNettyMessageClientSender(
                        _transportMessageEncoder,
                        channel);
                    //设置channel属性
                    channel.GetAttribute(messageSenderKey).Set(messageSender);
                    channel.GetAttribute(origEndPointKey).Set(k);
                    //创建客户端
                    var client = new TransportClient(_loggerFactory.CreateLogger<TransportClient>(), messageSender, messageListener);
                    return client;
                }
                )).Value;//返回实例
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
            return new NettyClient(_loggerFactory.CreateLogger<NettyClient>(), transportClient, timeout, url);
        }
        catch
        {
            //移除
            _clients.TryRemove(key, out _);
            //var ipEndPoint = endPoint as IPEndPoint;
            ////标记这个地址是失败的请求
            //if (ipEndPoint != null)
            //    await _healthCheckService.MarkFailure(new IpAddressModel(ipEndPoint.Address.ToString(), ipEndPoint.Port));
            throw;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var client in _clients.Values)
        {
            (client as IDisposable)?.Dispose();
        }
    }


    private Bootstrap GetBootstrap(URL url)
    {
        IEventLoopGroup group;

        var bootstrap = new Bootstrap();
        var libuv = url.GetParameter("Libuv", false);
        if (libuv)
        {
            group = new EventLoopGroup();
            bootstrap.Channel<TcpServerChannel>();
        }
        else
        {
            group = new MultithreadEventLoopGroup();
            bootstrap.Channel<TcpServerSocketChannel>();
        }

        bootstrap
            .Channel<TcpSocketChannel>()
            .Option(ChannelOption.TcpNodelay, true)
            .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
            .Group(group);

        bootstrap.Handler(new ActionChannelInitializer<ISocketChannel>(c =>
        {
            var pipeline = c.Pipeline;
            pipeline.AddLast(new LengthFieldPrepender(4));
            pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
            pipeline.AddLast(new TransportMessageChannelHandlerAdapter(
                _transportMessageDecoder
                ));
            pipeline.AddLast(new DefaultChannelHandler(this));
        }));
        return bootstrap;
    }

    protected class DefaultChannelHandler : ChannelHandlerAdapter
    {
        private readonly NettyClientPool _factory;

        public DefaultChannelHandler(NettyClientPool factory)
        {
            _factory = factory;
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            _factory._clients.TryRemove(context.Channel.GetAttribute(origEndPointKey).Get(), out var value);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var transportMessage = message as TransportMessage;

            var messageListener = context.Channel.GetAttribute(messageListenerKey).Get();
            var messageSender = context.Channel.GetAttribute(messageSenderKey).Get();
            messageListener.OnReceived(messageSender, transportMessage);
        }
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }
}

