using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Zooyard.DotNettyImpl.Adapter;
using Zooyard.DotNettyImpl.Codec;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.DotNettyImpl;

public class NettyClientPool : AbstractClientPool
{
    private readonly ITransportMessageEncoder _transportMessageEncoder;
    private readonly ITransportMessageDecoder _transportMessageDecoder;
    private readonly ILogger _logger;
    private readonly Bootstrap _bootstrap;
    private readonly IOptionsMonitor<NettyOption> _nettyOption;

    private static readonly AttributeKey<IMessageSender> messageSenderKey = AttributeKey<IMessageSender>.ValueOf(typeof(NettyClientPool), nameof(IMessageSender));
    private static readonly AttributeKey<IMessageListener> messageListenerKey = AttributeKey<IMessageListener>.ValueOf(typeof(NettyClientPool), nameof(IMessageListener));
    private static readonly AttributeKey<EndPoint> origEndPointKey = AttributeKey<EndPoint>.ValueOf(typeof(NettyClientPool), nameof(EndPoint));
    private readonly ConcurrentDictionary<EndPoint, Lazy<Task<ITransportClient>>> _clients = new();

    public const string TIMEOUT_KEY = "http_timeout";
    public const int DEFAULT_TIMEOUT = 5000;

    public NettyClientPool(ITransportMessageEncoder encoder, ITransportMessageDecoder decoder, IOptionsMonitor<NettyOption> nettyOption, ILogger<NettyClientPool> logger)
    {
        _transportMessageEncoder = encoder;
        _transportMessageDecoder = decoder;
        _logger = logger;
        _nettyOption = nettyOption;
        _bootstrap = GetBootstrap(nettyOption);
        _bootstrap.Handler(new ActionChannelInitializer<ISocketChannel>(c =>
        {
            var pipeline = c.Pipeline;
            pipeline.AddLast(new LengthFieldPrepender(4));
            pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
            pipeline.AddLast(new TransportMessageChannelHandlerAdapter(_transportMessageDecoder));
            pipeline.AddLast(new DefaultChannelHandler(this));
        }));
    }

    protected override async Task<IClient> CreateClient(URL url)
    {
        var key = new IPEndPoint(IPAddress.Parse(url.Host), url.Port);
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug($"准备为服务端地址：{key}创建客户端。");
        try
        {
            var transportClient = await _clients.GetOrAdd(key, k => new Lazy<Task<ITransportClient>>(async () =>
            {
                //客户端对象
                var bootstrap = _bootstrap;
                //异步连接返回channel
                var channel = await bootstrap.ConnectAsync(k);
                var messageListener = new MessageListener();
                //设置监听
                channel.GetAttribute(messageListenerKey).Set(messageListener);
                //实例化发送者
                var messageSender = new DotNettyMessageClientSender(_transportMessageEncoder, channel);
                //设置channel属性
                channel.GetAttribute(messageSenderKey).Set(messageSender);
                channel.GetAttribute(origEndPointKey).Set(k);
                //创建客户端
                var client = new TransportClient(messageSender, messageListener, _logger);
                return client;
            }
                )).Value;
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
            return new NettyClient(transportClient, timeout, url);
        }
        catch
        {
            //移除
            _clients.TryRemove(key, out var value);
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            (client as IDisposable)?.Dispose();
        }
    }

    private static Bootstrap GetBootstrap(IOptionsMonitor<NettyOption> nettyOption)
    {
        IEventLoopGroup group;

        var bootstrap = new Bootstrap();
        if (nettyOption.CurrentValue.Libuv)
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

    }
}

public class NettyOption
{
    public bool Libuv { get; set; } = false;
}

