using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RpcProviderCore.App_Start;
using System.Collections.Generic;
using System.IO;
using Thrift;
using Zooyard.Core.Extensions;
using Zooyard.Rpc.AkkaImpl.Extensions;
using Zooyard.Rpc.GrpcImpl.Extensions;
using Zooyard.Rpc.NettyImpl.Extensions;
using Zooyard.Rpc.ThriftImpl.Extensions;
using Zooyard.Rpc.HttpImpl.Extensions;
using Microsoft.Extensions.Hosting;
using Zooyard.Core;
using Microsoft.Extensions.Logging;
using Thrift.Transports;
using System;
using RpcContractWcf.HelloService;
using Zooyard.Rpc.Extensions;
using NLog;
using NLog.Extensions.Logging;
using NLog.Extensions.Hosting;

namespace RpcProviderCore
{
    public class ThriftServerOption
    {
        public int Port { get; set; }
        public int ClientTimeOut { get; set; }
        public bool UseBufferedSockets { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {

            var host = new HostBuilder()
                .UseConsoleLifetime()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    hostingContext.HostingEnvironment.ApplicationName = "MemberThrift.ServerHost";
                    hostingContext.HostingEnvironment.ContentRootPath = Directory.GetCurrentDirectory();
                    var env = hostingContext.HostingEnvironment;
                    //load json settings

                    var nlogSection = config.Build().GetSection("NLog");
                    LogManager.Configuration = new NLogLoggingConfiguration(nlogSection);

                })
                .UseNLog()
                .ConfigureServices((hostingContext, services) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + @"\App_Data\Config")
                .AddJsonFile("service.akka.json", false, true)
                .AddJsonFile("service.grpc.json", false, true)
                .AddJsonFile("service.netty.json", false, true)
                .AddJsonFile("service.thrift.json", false, true)
                .AddJsonFile("service.http.json", false, true)
                .AddJsonFile("service.json", false, true)
                .AddJsonFile("nlog.json", false, true);

                    var config = builder.Build();

                    services.Configure<AkkaServerOption>(config.GetSection("akka"));
                    services.Configure<GrpcServerOption>(config.GetSection("grpc"));
                    services.Configure<NettyServerOption>(config.GetSection("netty"));
                    services.Configure<ThriftServerOption>(config.GetSection("thrift"));
                    services.Configure<HttpServerOption>(config.GetSection("http"));
                    services.AddLogging();

                    services.AddTransient((serviceProvider) => "A");
                    services.AddAkkaServer();

                    services.AddTransient<RpcContractGrpc.HelloService.HelloServiceBase>((serviceProvider) => new HelloServiceGrpcImpl { ServiceName = "A" });
                    services.AddGrpcServer();


                    services.AddSingleton<IEventLoopGroup>((serviceProvider) =>
                    {
                        return new MultithreadEventLoopGroup(1);
                    });
                    services.AddSingleton<IEventLoopGroup>((serviceProvider) =>
                    {
                        return new MultithreadEventLoopGroup();
                    });

                    services.AddTransient((serviceProvider) => new HelloServiceNettyImpl { ServiceName = "A" });
                    services.AddNettyServer();


                    services.AddTransient<RpcContractThrift.HelloService.IAsync>((serviceProvider) => new HelloServiceThriftImpl { ServiceName = "A" });
                    services.AddTransient<ITAsyncProcessor, RpcContractThrift.HelloService.AsyncProcessor>();
                    services.AddSingleton<TServerTransport>((serviceProvider) =>
                    {
                        var option = serviceProvider.GetService<IOptionsMonitor<ThriftServerOption>>().CurrentValue;
                        return new Thrift.Transports.Server.TServerSocketTransport(option.Port, option.ClientTimeOut, option.UseBufferedSockets);
                    });
                    services.AddSingleton<Thrift.Protocols.ITProtocolFactory>(new Thrift.Protocols.TCompactProtocol.Factory());

                    services.AddThriftServer();

                    services.AddHttpServer<Startup>(args);

                    services.AddTransient<IHelloServiceWcf, HelloServiceWcfImpl>();

                    services.AddHostedService<ZoolandHostedService>();
                    //services.AddZoolandServer();

                })
                .ConfigureLogging((hostingContext, logging) =>
                {

                }).Build();

            using (host)
            {
                host.Run();
            }
        }
    }
}
