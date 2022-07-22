using DotNetty.Transport.Channels;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Extensions.Hosting;
using NLog.Extensions.Logging;
using Thrift.Processor;
using Zooyard;
//using Zooyard.Extensions;
//using RpcContractWcf.HelloService;
//using Zooyard.Rpc.Extensions;
using Zooyard.Rpc.GrpcImpl;
//using Zooyard.Rpc.AkkaImpl.Extensions;
//using Zooyard.Rpc.GrpcImpl.Extensions;
//using Zooyard.Rpc.HttpImpl.Extensions;
//using Zooyard.Rpc.NettyImpl.Extensions;
using Zooyard.Rpc.ThriftImpl;
//using Zooyard.Rpc.ThriftImpl.Extensions;

namespace RpcProviderCore;

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

            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "config");
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                //.AddJsonFile("service.akka.json", false, true)
                .AddJsonFile("service.grpc.json", false, true)
                .AddJsonFile("service.netty.json", false, true)
                .AddJsonFile("service.thrift.json", false, true)
                .AddJsonFile("service.http.json", false, true)
                .AddJsonFile("service.json", false, true)
                .AddJsonFile("nlog.json", false, true);

            var config = builder.Build();

            //ZooyardLogManager.UseConsoleLogging(Zooyard.Logging.LogLevel.Debug);

            //services.Configure<AkkaServerOption>(config.GetSection("akka"));
            services.Configure<GrpcServerOption>(config.GetSection("grpc"));
            services.Configure<NettyServerOption>(config.GetSection("netty"));
            //services.Configure<ThriftServerOption>(config.GetSection("thrift"));
            services.Configure<HttpServerOption>(config.GetSection("http"));
            services.AddLogging();

            ////实现注册接口代码
            //services.AddSingleton<IRegistryService>((provider)=> {
            //    return default;
            //});

            services.AddTransient((serviceProvider) => "A");
            //services.AddAkkaServer();

            //services.AddSingleton<ClientInterceptor, ClientGrpcInterceptor>();
            services.AddSingleton<ServerInterceptor, ServerGrpcInterceptor>();



            services.AddTransient<RpcContractGrpc.HelloService.HelloServiceBase, HelloServiceGrpcImpl>();
            //services.AddGrpcServer();
           


            services.AddSingleton<IEventLoopGroup>((serviceProvider) =>
            {
                return new MultithreadEventLoopGroup(1);
            });
            services.AddSingleton<IEventLoopGroup>((serviceProvider) =>
            {
                return new MultithreadEventLoopGroup();
            });

            services.AddTransient((serviceProvider) => new HelloServiceNettyImpl { ServiceName = "A" });
            //services.AddNettyServer();


            services.AddTransient<RpcContractThrift.HelloService.IAsync>((serviceProvider) => new HelloServiceThriftImpl { ServiceName = "A" });
            services.AddTransient<ITAsyncProcessor, RpcContractThrift.HelloService.AsyncProcessor>();
            //services.AddSingleton<TServerTransport>((serviceProvider) =>
            //{
            //    var option = serviceProvider.GetService<IOptionsMonitor<ThriftServerOption>>().CurrentValue;

            //    return new TServerSocketTransport(option.Port, option.Configuration, option.ClientTimeOut);
            //});
            //services.AddSingleton<TProtocolFactory>(new TBinaryProtocol.Factory());

            //services.AddSingleton<TTransportFactory>(new TFramedTransport.Factory());
            //services.AddSingleton<TTransportFactory>(new TBufferedTransport.Factory());


            //services.AddSingleton<TServer>(serviceProvider =>
            //{
            //    var processor = serviceProvider.GetService<ITAsyncProcessor>();
            //    var serverTransport = serviceProvider.GetService<TServerTransport>();
            //    var transportFactory = serviceProvider.GetService<TTransportFactory>();
            //    var protocolFactory = serviceProvider.GetService<TProtocolFactory>();
            //    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            //    var threadConfig = new TThreadPoolAsyncServer.Configuration();
            //    var server = new TThreadPoolAsyncServer(
            //         processorFactory: new TSingletonProcessorFactory(processor),
            //         serverTransport: serverTransport,
            //         inputTransportFactory: transportFactory,
            //         outputTransportFactory: transportFactory,
            //         inputProtocolFactory: protocolFactory,
            //         outputProtocolFactory: protocolFactory,
            //         threadConfig: threadConfig,
            //         logger: loggerFactory.CreateLogger<TThreadPoolAsyncServer>());
            //    return server;
            //});

            //services.AddSingleton<TServer>(serviceProvider =>
            //{
            //    var processor = serviceProvider.GetService<ITAsyncProcessor>();
            //    var serverTransport = serviceProvider.GetService<TServerTransport>();
            //    var transportFactory = serviceProvider.GetService<TTransportFactory>();
            //    var protocolFactory = serviceProvider.GetService<TProtocolFactory>();
            //    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            //    return new TSimpleAsyncServer(
            //        itProcessorFactory: new TSingletonProcessorFactory(processor),
            //        serverTransport: serverTransport,
            //        inputTransportFactory: transportFactory,
            //        outputTransportFactory: transportFactory,
            //        inputProtocolFactory: protocolFactory,
            //        outputProtocolFactory: protocolFactory,
            //        logger: loggerFactory.CreateLogger<TSimpleAsyncServer>());
            //});

            //services.AddThriftServer();

            //services.AddHttpServer<Startup>(args);

            //services.AddTransient<IHelloServiceWcf, HelloServiceWcfImpl>();


            //services.AddZoolandServer();
            //services.AddHostedService<ZoolandHostedService>();

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
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        context.Options.Headers.Add(new Metadata.Entry("test", "test"));
        return base.AsyncServerStreamingCall(request, context, continuation);
    }
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            
            var response = await continuation(request, context);
            return response;
        }
        catch
        {
            throw;
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
        catch
        {
            throw;
        }
    }

    // 服务端流式调用拦截
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation) 
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch
        {
            throw;
        }
    }

    // 双向流调用拦截
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation) 
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch
        {
            throw;
        }
    }
}
//public class ThriftServerOption
//{
//    public int Port { get; set; }
//    public TConfiguration Configuration { get; set; } = new TConfiguration();
//    public int ClientTimeOut { get; set; }
//}
