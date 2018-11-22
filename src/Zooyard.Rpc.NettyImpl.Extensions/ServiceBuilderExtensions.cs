using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zooyard.Core;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;

namespace Zooyard.Rpc.NettyImpl.Extensions
{
    public class NettyOption
    {
        public IEnumerable<NettyPortocolOption> Protocols { get; set; }
    }
    public class NettyPortocolOption
    {
        public string Name { get; set; }
        public string EventLoopGroupType { get; set; }
        public string ChannelType { get; set; }
        //public IEnumerable<string> Handlers { get; set; }
    }

    public class NettyServerOption
    {
        public string ServerChannelType { get; set; }
        public string ServiceType { get; set; }
        public int Port { get; set; } = 12121;
        public bool IsSsl { get; set; } = false;
        public string Pfx { get; set; } = "";
        public string Pwd { get; set; } = "";
    }
    

    public static class ServiceBuilderExtensions
    {
        public static void AddNettyClient(this IServiceCollection services, IDictionary<string, IEnumerable<IChannelHandler>> handlersDic)
        {
            services.AddSingleton((serviceProvider) => 
            {
                var option = serviceProvider.GetService<IOptions<NettyOption>>().Value;
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                

                var nettyProtocols = new Dictionary<string, NettyProtocol>();
                foreach (var item in option.Protocols)
                {
                    IEnumerable<IChannelHandler> handlers = new List<IChannelHandler>();
                    if (handlersDic?.ContainsKey(item.Name)??false)
                    {
                        handlers = handlersDic[item.Name];
                    }

                    var value = new NettyProtocol
                    {
                        EventLoopGroupType = Type.GetType(item.EventLoopGroupType),
                        ChannelType = Type.GetType(item.ChannelType),
                        ChannelHandlers= handlers
                    };
                    nettyProtocols.Add(item.Name, value);
                }
                
                var pool = new NettyClientPool(
                    nettyProtocols: nettyProtocols,
                    loggerFactory: loggerFactory
                );

                return pool;
            });

        }

        public static void AddNettyServer(this IServiceCollection services, IEnumerable<IChannelHandler> handlers = null)
        {

            services.AddSingleton<TcpServerSocketChannel>();
            
            services.AddSingleton((serviceProvider)=> 
            {
                var option = serviceProvider.GetService<IOptions<NettyServerOption>>().Value;
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                

            
                var service = serviceProvider.GetService(Type.GetType(option.ServiceType));
                var serverChannel = serviceProvider.GetService(Type.GetType(option.ServerChannelType)) as IServerChannel;

                var groups = serviceProvider.GetServices<IEventLoopGroup>();
                
                return new NettyServer(groups.ElementAt(0), groups.ElementAt(1), serverChannel, handlers, service, option.Port, option.IsSsl, option.Pfx, option.Pwd, loggerFactory);
            });
            

        }
    }
}
