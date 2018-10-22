using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
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
    public class NettyClient : AbstractClient
    {
        public const string QUIETPERIOD_KEY = "quietPeriod";
        public const int DEFAULT_QUIETPERIOD = 100;
        public const string TIMEOUT_KEY = "timeout";
        public const int DEFAULT_TIMEOUT = 1;

        public override URL Url { get; }
        public IEventLoopGroup EventLoopGroup { get; private set; }
        public IChannel Client { get; private set; }
        public IMessageListener MessageListener { get; private set; }

        // private static readonly AttributeKey<EndPoint> origEndPointKey = AttributeKey<EndPoint>.ValueOf(typeof(NettyClientPool), nameof(EndPoint));

        public NettyClient(IEventLoopGroup eventLoopGroup, IChannel client,IMessageListener messageListener ,URL url)
        {
            this.EventLoopGroup = eventLoopGroup;
            this.Client = client;
            this.MessageListener = messageListener;
            this.Url = url;
        }


        public override IInvoker Refer()
        {
            this.Open();
            //thrift client service
            return new NettyInvoker(Client, MessageListener);
        }

        public override void Open()
        {
            if (!Client.Open || !Client.Active)
            {
                var k = new IPEndPoint(IPAddress.Parse(Url.Host), Url.Port);
                Client.ConnectAsync(k).GetAwaiter().GetResult();
                //Client.GetAttribute(origEndPointKey).Set(k);
            }
        }

        public override void Close()
        {
            if (Client.Active || Client.Open)
            {
                Client.DisconnectAsync().GetAwaiter().GetResult();
            }
        }

        public override void Dispose()
        {
            Close();
            Client.CloseAsync().GetAwaiter().GetResult();
            if (!EventLoopGroup.IsShutdown || !EventLoopGroup.IsTerminated)
            {
                var quietPeriod = Url.GetParameter(QUIETPERIOD_KEY, DEFAULT_QUIETPERIOD);
                var timeout = Url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
                EventLoopGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(quietPeriod), TimeSpan.FromSeconds(timeout)).GetAwaiter().GetResult();
            }
        }
        

        
    }

    
}
