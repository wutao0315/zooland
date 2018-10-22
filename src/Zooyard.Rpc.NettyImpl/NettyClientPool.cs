﻿using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
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

        public IDictionary<string, NettyProtocol> NettyProtocol { get; set; }
        //public Type EventLoopGroupType { get; set; }
        //public Type ChannelType { get; set; }
        //public IEventLoopGroup EventLoopGroup { get; set; }
        //public IChannel TheChannel { get; set; }

        private static readonly AttributeKey<IMessageListener> messageListenerKey = AttributeKey<IMessageListener>.ValueOf(typeof(NettyClientPool), nameof(IMessageListener));
        

        protected override IClient CreateClient(URL url)
        {
            var protocol = NettyProtocol[url.Protocol];

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
                .Group(group)
                .ChannelFactory(() => clientChannel)


                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    if (cert != null)
                    {
                        pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                    }
                    pipeline.AddLast(new LoggingHandler());
                        //pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        //pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                    pipeline.AddLast(new LengthFieldPrepender(4));
                    pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));

                    pipeline.AddLast(new ClientHandler());
                }));
            
            //var client = bootstrap.BindAsync(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)).GetAwaiter().GetResult();
            var client = bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)).GetAwaiter().GetResult();
            var messageListener = new MessageListener();
            client.GetAttribute(messageListenerKey).Set(messageListener);
            

            //IChannel clientChannel = bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(url.Host), url.Port)).GetAwaiter().GetResult();
            return new NettyClient(group, client, messageListener, url);
        }

        protected class ClientHandler : ChannelHandlerAdapter
        {
 

            public override void ChannelRead(IChannelHandlerContext context, object message)
            {
                var byteBuffer = message as IByteBuffer;
                //var result = "";
                //if (byteBuffer != null)
                //{
                //    result = byteBuffer.ToString(Encoding.UTF8);
                //    Console.WriteLine($"Received from server:{result}");
                //}
                //var transportMessage = JsonConvert.DeserializeObject<TransportMessage>(result);

                if (byteBuffer == null)
                {
                    throw new Exception("byte buffer is null");
                }

                var bytes = new byte[byteBuffer.ReadableBytes];
                byteBuffer.ReadBytes(bytes);
                var transportMessage = bytes.Desrialize<TransportMessage>();
                var messageListener = context.Channel.GetAttribute(messageListenerKey).Get();
                messageListener.OnReceived(transportMessage);
            }

            public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
            {
                Console.WriteLine("Exception: " + exception);
                context.CloseAsync();
            }
        }
    }

    public class NettyProtocol
    {
        public Type EventLoopGroupType { get; set; }
        public Type ChannelType { get; set; }
    }
}
