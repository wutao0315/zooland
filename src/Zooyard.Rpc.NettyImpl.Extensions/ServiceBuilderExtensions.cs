﻿using DotNetty.Transport.Channels;
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
    }

    public class NettyServerOption
    {
        public string ServiceType { get; set; }
        public string Url { get; set; }
        public bool IsSsl { get; set; } = false;
        public string Pfx { get; set; } = "";
        public string Pwd { get; set; } = "";
    }
    

    public static class ServiceBuilderExtensions
    {
        public static void AddNettyClient(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvider) => 
            {
                var option = serviceProvider.GetService<IOptions<NettyOption>>().Value;
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                

                var nettyProtocols = new Dictionary<string, NettyProtocol>();
                foreach (var item in option.Protocols)
                {
                    

                    var value = new NettyProtocol
                    {
                        EventLoopGroupType = Type.GetType(item.EventLoopGroupType),
                        ChannelType = Type.GetType(item.ChannelType)
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

        public static void AddNettyServer(this IServiceCollection services)
        {
            
            
            services.AddSingleton<IServer>((serviceProvider)=> 
            {
                var option = serviceProvider.GetService<IOptions<NettyServerOption>>().Value;
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

                var url = URL.valueOf(option.Url);

                var service = serviceProvider.GetService(Type.GetType(option.ServiceType));
     
                
                
                return new NettyServer(url, service,  option.IsSsl, option.Pfx, option.Pwd, loggerFactory);
            });
            

        }
    }
}