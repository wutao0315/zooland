using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using Thrift;
using Zooyard.Core.Extensions;
//using Zooyard.Rpc.AkkaImpl.Extensions;
using Zooyard.Rpc.GrpcImpl.Extensions;
using Zooyard.Rpc.NettyImpl.Extensions;
//using Zooyard.Rpc.ThriftImpl.Extensions;
using Zooyard.Rpc.HttpImpl.Extensions;
using Microsoft.Extensions.Hosting;
using Zooyard.Core;
using Microsoft.Extensions.Logging;
using Thrift.Transports;
using System;
//using RpcContractWcf.HelloService;
using Zooyard.Rpc.Extensions;
using NLog;
using NLog.Extensions.Logging;
using NLog.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Zooyard.Rpc.GrpcImpl;
using Grpc.Core.Interceptors;
using Grpc.Core;
using System.Threading.Tasks;

namespace RpcProviderCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) => {
                hostingContext.HostingEnvironment.ApplicationName = "RpcProviderCore";
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
            //.AddJsonFile("service.akka.json", false, true)
            .AddJsonFile("service.grpc.json", false, true)
            .AddJsonFile("service.netty.json", false, true)
            //.AddJsonFile("service.thrift.json", false, true)
            .AddJsonFile("service.http.json", false, true)
            .AddJsonFile("service.json", false, true)
            .AddJsonFile("nlog.json", false, true);

                var config = builder.Build();

                //services.Configure<AkkaServerOption>(config.GetSection("akka"));
                services.Configure<GrpcServerOption>(config.GetSection("grpc"));
                services.Configure<NettyServerOption>(config.GetSection("netty"));
                //services.Configure<ThriftServerOption>(config.GetSection("thrift"));
                services.Configure<HttpServerOption>(config.GetSection("http"));
                services.AddLogging();

                //实现注册接口代码
                services.AddSingleton<IRegistryService>((provider)=> {
                    return default(IRegistryService);
                });

                services.AddTransient((serviceProvider) => "A");
                //services.AddAkkaServer();

                //services.AddSingleton<ClientInterceptor, ClientGrpcInterceptor>();
                services.AddSingleton<ServerInterceptor, ServerGrpcInterceptor>();

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


                //services.AddTransient<RpcContractThrift.HelloService.IAsync>((serviceProvider) => new HelloServiceThriftImpl { ServiceName = "A" });
                //services.AddTransient<ITAsyncProcessor, RpcContractThrift.HelloService.AsyncProcessor>();
                //services.AddSingleton<TServerTransport>((serviceProvider) =>
                //{
                //    var option = serviceProvider.GetService<IOptionsMonitor<ThriftServerOption>>().CurrentValue;
                //    return new Thrift.Transports.Server.TServerSocketTransport(option.Port, option.ClientTimeOut, option.UseBufferedSockets);
                //});
                //services.AddSingleton<Thrift.Protocols.ITProtocolFactory>(new Thrift.Protocols.TCompactProtocol.Factory());

                //services.AddThriftServer();

                services.AddHttpServer<Startup>(args);

                //services.AddTransient<IHelloServiceWcf, HelloServiceWcfImpl>();

                services.AddHostedService<ZoolandHostedService>();
                //services.AddZoolandServer();

            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }

    public class ClientGrpcInterceptor : ClientInterceptor
    {

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = new Metadata();
            metadata.Add(new Metadata.Entry("test", "test"));
            var options = context.Options.WithHeaders(metadata);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            var response = continuation(request, context);
            return response;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = new Metadata();
            metadata.Add(new Metadata.Entry("test", "test"));
            var options = context.Options.WithHeaders(metadata);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            var response = continuation(request, context);

            var responseAsync = response.ResponseAsync.ContinueWith<TResponse>((r) => r.Result);
            return new AsyncUnaryCall<TResponse>(responseAsync, response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);

        }
    }
    public class ServerGrpcInterceptor : ServerInterceptor
    {
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = new Metadata();
            metadata.Add(new Metadata.Entry("test", "test"));
            var options = context.Options.WithHeaders(metadata);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            var response = continuation(request, context);
            return response;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = new Metadata();
            metadata.Add(new Metadata.Entry("test", "test"));
            var options = context.Options.WithHeaders(metadata);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            var response = continuation(request, context);

            var responseAsync = response.ResponseAsync.ContinueWith<TResponse>((r) => r.Result);
            return new AsyncUnaryCall<TResponse>(responseAsync, response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);

        }
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var response = await continuation(request, context);
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        // 客户端流式调用拦截
        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var response = await continuation(requestStream, context);
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 服务端流式调用拦截
        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation) 
        {
            try
            {
                await continuation(request, responseStream, context);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 双向流调用拦截
        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation) 
        {
            try
            {
                await continuation(requestStream, responseStream, context);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
    public class ThriftServerOption
    {
        public int Port { get; set; }
        public int ClientTimeOut { get; set; }
        public bool UseBufferedSockets { get; set; }
    }
}
