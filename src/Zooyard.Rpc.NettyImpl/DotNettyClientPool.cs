using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Support;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class DotNettyClientPool : AbstractClientPool
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyClientPool));

        

        public DotNettyClientPool() 
        {
            NettyRemotingClient.Instance.Init().GetAwaiter().GetResult();
        }

        protected override async Task<IClient> CreateClient(URL url)
        {
            var channel = await NettyRemotingClient.Instance.ClientChannelManager.AcquireChannel($"{url.Host}:${url.Port}");


            return new DotNettyClient(channel,  url);
            //return new DotNettyClient(group, client, messageListener, timeout, url);
        }
    }
}
