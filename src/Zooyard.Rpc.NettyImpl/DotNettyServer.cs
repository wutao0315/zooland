﻿using DotNetty.Buffers;
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
using System.Threading;
using System.Threading.Tasks;
using Zooyard;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl
{
    public class DotNettyServer : AbstractServer
    {
        //UseLibuv
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyServer));
        
        protected internal volatile IChannel ServerChannel;
        internal readonly ConcurrentSet<IChannel> ConnectionGroup;

        public DotNettyServer(URL config,
            IRegistryService registryService)
            : base(registryService)
        {
        }

        public override async Task DoExport(CancellationToken cancellationToken)
        {
            Logger().LogDebug($"ready to start the server on port:{8098}.");

            Logger().LogInformation($"Started the netty server ...");
            Console.WriteLine($"Started the netty server ...");
            
        }

        public override async Task DoDispose()
        {

        }
    }
}