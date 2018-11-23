using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class NettyClientPool : AbstractClientPool
    {
        public const string SSL_KEY = "ssl";
        public const bool DEFAULT_SSL = false;
        public const string PWD_KEY = "pwd";
        public const string DEFAULT_PWD = "password";
        public const string PFX_KEY = "pfx";
        public const string DEFAULT_PFX = "dotnetty.com";

        private readonly IDictionary<string, NettyProtocol> _nettyProtocols;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        public NettyClientPool(IDictionary<string, NettyProtocol> nettyProtocols, ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<NettyClientPool>();
            _nettyProtocols = nettyProtocols;
        }

        protected override IClient CreateClient(URL url)
        {
            var protocol = _nettyProtocols[url.Protocol];

            IEventLoopGroup group = protocol.EventLoopGroupType
                   .GetConstructor(new Type[] { })
                   .Invoke(new object[] { }) as IEventLoopGroup;

            IChannel clientChannel = protocol.ChannelType
                   .GetConstructor(new Type[] { })
                   .Invoke(new object[] { }) as IChannel;

            var isSsl = url.GetParameter(SSL_KEY, DEFAULT_SSL);

            X509Certificate2 cert = null;
            string targetHost = null;
            if (isSsl)
            {
                var pfx = url.GetParameter(PFX_KEY, DEFAULT_PFX);
                var pwd = url.GetParameter(PWD_KEY, DEFAULT_PWD);
                cert = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{pfx}.pfx"), pwd);
                targetHost = cert.GetNameInfo(X509NameType.DnsName, false);
            }


            var bootstrap = new Bootstrap();
            bootstrap
                .ChannelFactory(() => clientChannel)
                //.Option(ChannelOption.SoReuseaddr, Settings.TcpReuseAddr)
                //.Option(ChannelOption.SoKeepalive, Settings.TcpKeepAlive)
                //.Option(ChannelOption.TcpNodelay, Settings.TcpNoDelay)
                //.Option(ChannelOption.ConnectTimeout, Settings.ConnectTimeout)
                .Option(ChannelOption.AutoRead, true)
                .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .Group(group)
                .Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    if (cert != null)
                    {
                        pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                    }

                    //pipeline.AddLast(new LoggingHandler());
                    //pipeline.AddLast(new LengthFieldPrepender(4));
                    //pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
                    pipeline.AddLast(protocol.ChannelHandlers?.ToArray());
                    pipeline.AddLast(new ClientHandler(ReceivedMessage, _loggerFactory));

                    
                }));

            //if (Settings.ReceiveBufferSize.HasValue) client.Option(ChannelOption.SoRcvbuf, Settings.ReceiveBufferSize.Value);
            //if (Settings.SendBufferSize.HasValue) client.Option(ChannelOption.SoSndbuf, Settings.SendBufferSize.Value);
            //if (Settings.WriteBufferHighWaterMark.HasValue) client.Option(ChannelOption.WriteBufferHighWaterMark, Settings.WriteBufferHighWaterMark.Value);
            //if (Settings.WriteBufferLowWaterMark.HasValue) client.Option(ChannelOption.WriteBufferLowWaterMark, Settings.WriteBufferLowWaterMark.Value);


            var client = bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)).GetAwaiter().GetResult();
            var messageListener = new MessageListener();
            client.GetAttribute(messageListenerKey).Set(messageListener);


            return new NettyClient(group, client, messageListener, url, _loggerFactory);
        }

        private static readonly AttributeKey<IMessageListener> messageListenerKey = AttributeKey<IMessageListener>.ValueOf(typeof(NettyClientPool), nameof(IMessageListener));
        
        public void ReceivedMessage(IChannelHandlerContext context, TransportMessage transportMessage)
        {
            var ml = context.Channel.GetAttribute(messageListenerKey).Get();
            ml.OnReceived(transportMessage);
        }
    }

    internal class ClientHandler : ChannelHandlerAdapter
    {
        private readonly Action<IChannelHandlerContext,TransportMessage> _receviedAction;
        private ILogger _logger;
        public ClientHandler(Action<IChannelHandlerContext,TransportMessage> receviedAction, ILoggerFactory loggerFactory)
        {
            _receviedAction = receviedAction;
            _logger = loggerFactory.CreateLogger<ClientHandler>();
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is IByteBuffer buffer)
            {
                var bytes = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(bytes);
                var transportMessage = bytes.Desrialize<TransportMessage>();
                context.FireChannelRead(transportMessage);
                ReferenceCountUtil.SafeRelease(buffer);
                _receviedAction(context, transportMessage);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine($"Exception: {exception.Message}");
            _logger.LogError(exception, exception.Message);
            context.CloseAsync();
        }
    }

    public class NettyProtocol
    {
        public Type EventLoopGroupType { get; set; }
        public Type ChannelType { get; set; }
        public IEnumerable<IChannelHandler> ChannelHandlers { get; set; }
    }
}
