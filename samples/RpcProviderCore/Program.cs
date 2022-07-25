﻿using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Extensions.Hosting;
using NLog.Extensions.Logging;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Thrift;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;
using Thrift.Transport.Server;
using Zooyard;
using Zooyard.Rpc.GrpcImpl;
using Zooyard.Rpc.NettyImpl;
using Zooyard.Rpc.ThriftImpl;
using Zooyard.Rpc.ThriftImpl.Header;

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

            services.AddThriftServer();

            services.AddHttpServer<Startup>(args);

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
public static class ServiceBuilderExtensions 
{
    public static void AddGrpcServer(this IServiceCollection services)
    {
        services.AddSingleton<IEnumerable<ServerServiceDefinition>>((serviceProvder) =>
        {
            var option = serviceProvder.GetRequiredService<IOptionsMonitor<GrpcServerOption>>().CurrentValue;
            var result = new List<ServerServiceDefinition>();

            foreach (var item in option.Services)
            {
                var contractType = Type.GetType(item.Key);
                var implType = Type.GetType(item.Value);
                var implValue = serviceProvder.GetService(implType);
                var definition = contractType.GetMethod("BindService", new[] { implType })
                .Invoke(null, new[] { implValue }) as ServerServiceDefinition;
                result.Add(definition);
            }

            return result;
        });

        services.AddSingleton<IEnumerable<ServerPort>>((serviceProvder) =>
        {
            var option = serviceProvder.GetRequiredService<IOptionsMonitor<GrpcServerOption>>().CurrentValue;
            var result = new List<ServerPort>();
            foreach (var item in option.ServerPorts)
            {
                var defaultCredential = ServerCredentials.Insecure;
                if (!string.IsNullOrWhiteSpace(item.Credentials)
                && item.Credentials != "default"
                && item.Credentials != "Insecure"
                )
                {
                    var credentialType = Type.GetType(item.Credentials);
                    defaultCredential = serviceProvder.GetService(credentialType) as ServerCredentials;
                }
                var port = new ServerPort(item.Host, item.Port, defaultCredential);
                result.Add(port);
            }
            return result;
        });

        services.AddSingleton<GrpcServer>();
    }
    public static void AddNettyServer(this IServiceCollection services)
    {
        services.AddTransient<NettyServer>((serviceProvider) =>
        {
            var option = serviceProvider.GetRequiredService<IOptionsMonitor<NettyServerOption>>().CurrentValue;
            var url = URL.ValueOf(option.Url);

            var service = serviceProvider.GetService(Type.GetType(option.ServiceType));

            return new NettyServer(url, service, option.IsSsl, option.Pfx, option.Pwd);
        });
    }
    public static void AddThriftServer(this IServiceCollection services)
    {
        services.AddSingleton<ThriftServer>();
    }
    public static void AddHttpServer<Startup>(this IServiceCollection services, string[] args)
        where Startup : class
    {
        services.AddSingleton((serviceProvider) =>
        {
            var option = serviceProvider.GetRequiredService<IOptionsMonitor<HttpServerOption>>().CurrentValue;
            var host = new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>()
            .UseUrls(option.Urls.ToArray())
            .Build();

            return host;
        });

        services.AddSingleton<HttpServer>();
    }
}
public class ZoolandHostedService : IHostedService
{
    private readonly GrpcServer _grpcServer;
    private readonly ThriftServer _thriftServer;
    private readonly NettyServer _nettyServer;
    private readonly HttpServer _httpServer;

    public ZoolandHostedService(GrpcServer grpcServer, ThriftServer thriftServer, NettyServer nettyServer, HttpServer httpServer)
    {
        _grpcServer = grpcServer;
        _thriftServer = thriftServer;
        _nettyServer = nettyServer;
        _httpServer = httpServer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Zooland started...");
        try
        {
            await _grpcServer.Start(cancellationToken).ConfigureAwait(false);
            await _thriftServer.Start(cancellationToken).ConfigureAwait(false);
            await _nettyServer.Start(cancellationToken).ConfigureAwait(false);
            await _httpServer.Start(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}:{ex.StackTrace}");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Zooland stopped...");
        try
        {
            await _grpcServer.Stop().ConfigureAwait(false);
            await _thriftServer.Stop().ConfigureAwait(false);
            await _nettyServer.Stop().ConfigureAwait(false);
            await _httpServer.Stop().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}:{ex.StackTrace}");
        }
    }
}
public class GrpcServer
{
    private Server _server;
    public GrpcServer(IEnumerable<ServerServiceDefinition> services,
        IEnumerable<ServerPort> ports,
        IEnumerable<ServerInterceptor> interceptors)
    {
        _server = new Server();

        foreach (var item in services)
        {
            if (interceptors?.Count() > 0)
            {
                item.Intercept(interceptors.ToArray());
            }
            _server.Services.Add(item);
        }
        foreach (var item in ports)
        {
            _server.Ports.Add(item);
        }
    }


    public async Task Start(CancellationToken cancellationToken)
    {
        //开启服务
        _server.Start();
        await Task.CompletedTask;
        var ports = _server.Ports.Select(w => w.Port);
        Console.WriteLine($"Started the grpc server on{string.Join(",", ports)} ...");
    }

    public async Task Stop()
    {
        try
        {
            await _server.ShutdownAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + ex.StackTrace);
        }
    }
}

public class NettyServer
{
    private readonly object _service;
    private readonly bool _isSsl = false;
    private readonly string _pfx = "dotnetty.com";
    private readonly string _pwd = "password";

    private readonly IEventLoopGroup _serverEventLoopGroup;
    protected internal volatile IChannel ServerChannel;
    internal readonly ConcurrentSet<IChannel> ConnectionGroup;

    public NettyServer(URL config,
        object service,
        bool isSsl,
        string pfx,
        string pwd)
    {
        Settings = NettyTransportSettings.Create(config);
        ConnectionGroup = new ConcurrentSet<IChannel>();

        _serverEventLoopGroup = new MultithreadEventLoopGroup(Settings.ServerSocketWorkerPoolSize);

        _service = service;
        _isSsl = isSsl;
        _pfx = pfx;
        _pwd = pwd;
    }
    internal NettyTransportSettings Settings { get; }


    private ServerBootstrap ServerFactory()
    {
        X509Certificate2 tlsCertificate = null;
        if (_isSsl)
        {
            tlsCertificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{_pfx}.pfx"), _pwd);
        }

        var addressFamily = Settings.DnsUseIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

        var server = new ServerBootstrap()
            .Group(_serverEventLoopGroup)
            .Option(DotNetty.Transport.Channels.ChannelOption.SoReuseaddr, Settings.TcpReuseAddr)
            .Option(DotNetty.Transport.Channels.ChannelOption.SoKeepalive, Settings.TcpKeepAlive)
            .Option(DotNetty.Transport.Channels.ChannelOption.TcpNodelay, Settings.TcpNoDelay)
            .Option(DotNetty.Transport.Channels.ChannelOption.AutoRead, true)
            .Option(DotNetty.Transport.Channels.ChannelOption.SoBacklog, Settings.Backlog)
            .Option(DotNetty.Transport.Channels.ChannelOption.Allocator, Settings.EnableBufferPooling ? (IByteBufferAllocator)PooledByteBufferAllocator.Default : UnpooledByteBufferAllocator.Default)
            .ChannelFactory(() => Settings.EnforceIpFamily
                ? new TcpServerSocketChannel(addressFamily)
                : new TcpServerSocketChannel())
            .ChildHandler(new ActionChannelInitializer<TcpSocketChannel>((channel =>
            {
                var pipeline = channel.Pipeline;

                if (tlsCertificate != null)
                {
                    pipeline.AddLast("tls", TlsHandler.Server(tlsCertificate));
                }

                //pipeline.AddLast(new DotNetty.Handlers.Logging.LoggingHandler("SRV-CONN"));
                //pipeline.AddLast(new LengthFieldPrepender(4));
                //pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
                pipeline.AddLast(new NettyLoggingHandler());
                SetInitialChannelPipeline(channel);
                //pipeline.AddLast(_channelHandlers?.ToArray());
                pipeline.AddLast(new ServerHandler(this, _service));

            })));

        if (Settings.ReceiveBufferSize.HasValue) server.Option(DotNetty.Transport.Channels.ChannelOption.SoRcvbuf, Settings.ReceiveBufferSize.Value);
        if (Settings.SendBufferSize.HasValue) server.Option(DotNetty.Transport.Channels.ChannelOption.SoSndbuf, Settings.SendBufferSize.Value);
        if (Settings.WriteBufferHighWaterMark.HasValue) server.Option(DotNetty.Transport.Channels.ChannelOption.WriteBufferHighWaterMark, Settings.WriteBufferHighWaterMark.Value);
        if (Settings.WriteBufferLowWaterMark.HasValue) server.Option(DotNetty.Transport.Channels.ChannelOption.WriteBufferLowWaterMark, Settings.WriteBufferLowWaterMark.Value);

        return server;
    }

    private void SetInitialChannelPipeline(IChannel channel)
    {
        var pipeline = channel.Pipeline;

        if (Settings.LogTransport)
        {
            pipeline.AddLast("Logger", new NettyLoggingHandler());
        }

        pipeline.AddLast("FrameDecoder", new LengthFieldBasedFrameDecoder(Settings.ByteOrder, Settings.MaxFrameSize, 0, 4, 0, 4, true));
        if (Settings.BackwardsCompatibilityModeEnabled)
        {
            pipeline.AddLast("FrameEncoder", new HeliosBackwardsCompatabilityLengthFramePrepender(4, false));
        }
        else
        {
            pipeline.AddLast("FrameEncoder", new LengthFieldPrepender(Settings.ByteOrder, 4, 0, false));
        }
    }
    public async Task Start(CancellationToken cancellationToken)
    {
        //Logger().LogDebug($"ready to start the server on port:{Settings.Port}.");

        var newServerChannel = await ServerFactory().BindAsync(IPAddress.Any, Settings.Port);

        // Block reads until a handler actor is registered
        // no incoming connections will be accepted until this value is reset
        // it's possible that the first incoming association might come in though

        //newServerChannel.Configuration.AutoRead = false;
        ConnectionGroup.TryAdd(newServerChannel);
        ServerChannel = newServerChannel;

        //Logger().LogInformation($"Started the netty server ...");
        Console.WriteLine($"Started the netty server ...");

    }

    public async Task Stop()
    {
        try
        {
            foreach (var channel in ConnectionGroup)
            {
                await channel.CloseAsync();
            }
            await ServerChannel?.CloseAsync();

            //var tasks = new List<Task>();
            //foreach (var channel in ConnectionGroup)
            //{
            //    tasks.Add(channel.CloseAsync());
            //}
            //var all = Task.WhenAll(tasks);
            //all.ConfigureAwait(false).GetAwaiter().GetResult();

            //var server = ServerChannel?.CloseAsync() ?? Task.CompletedTask;
            //server.ConfigureAwait(false).GetAwaiter().GetResult();

        }
        finally
        {
            // free all of the connection objects we were holding onto
            ConnectionGroup.Clear();
            // shutting down the worker groups can take up to 10 seconds each. Let that happen asnychronously.
            await _serverEventLoopGroup.ShutdownGracefullyAsync();
        }

    }


}
internal abstract class ServerCommonHandlers : ChannelHandlerAdapter
{
    //private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ServerCommonHandlers));
    protected readonly NettyServer Server;


    protected ServerCommonHandlers(NettyServer server)
    {
        Server = server;
    }


    public override void ChannelActive(IChannelHandlerContext context)
    {
        base.ChannelActive(context);
        if (!Server.ConnectionGroup.TryAdd(context.Channel))
        {
            //Logger().LogWarning($"Unable to ADD channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id}) to connection group. May not shut down cleanly.");
        }
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        base.ChannelInactive(context);
        if (!Server.ConnectionGroup.TryRemove(context.Channel))
        {
            //Logger().LogWarning($"Unable to REMOVE channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id}) from connection group. May not shut down cleanly.");
        }
    }

    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        base.ExceptionCaught(context, exception);
        //Logger().LogError(exception, $"Error caught channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id})");
    }

}
internal class ServerHandler : ServerCommonHandlers
{
    private readonly object _service;
    //private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ServerHandler));

    public ServerHandler(NettyServer server, object service) : base(server)
    {
        _service = service;
    }

    #region Overrides of ChannelHandlerAdapter
    public override void ChannelActive(IChannelHandlerContext context)
    {
        base.ChannelActive(context);
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        base.ChannelInactive(context);
    }

    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        //接收到消息
        //调用服务器端接口
        //将返回值编码
        //将返回值发送到客户端
        if (message is IByteBuffer buffer)
        {
            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);
            var transportMessage = bytes.Desrialize<TransportMessage>();
            context.FireChannelRead(transportMessage);
            ReferenceCountUtil.SafeRelease(buffer);

            var rpc = transportMessage.GetContent<RemoteInvokeMessage>();

            var methodName = rpc.Method;
            var arguments = rpc.Arguments;
            var types = (from item in arguments select item.GetType()).ToArray();
            var remoteInvoker = new RemoteInvokeResultMessage
            {
                ExceptionMessage = "",
                StatusCode = 200
            };
            try
            {
                var method = _service.GetType().GetMethod(methodName, types);
                var result = method.Invoke(_service, arguments);

                if (method.ReturnType == typeof(Task))
                {
                    remoteInvoker.Result = null;
                }
                else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var awaiter = result.GetType().GetMethod("GetAwaiter").Invoke(result, new object[] { });
                    var value = awaiter.GetType().GetMethod("GetResult").Invoke(awaiter, new object[] { });
                    remoteInvoker.Result = value;
                }
                else
                {
                    remoteInvoker.Result = result;
                }
            }
            catch (Exception ex)
            {
                remoteInvoker.ExceptionMessage = ex.Message;
                remoteInvoker.StatusCode = 500;
                //Logger().LogError(ex, ex.Message);
            }
            var resultData = TransportMessage.CreateInvokeResultMessage(transportMessage.Id, remoteInvoker);
            var sendByte = resultData.Serialize();
            var sendBuffer = Unpooled.WrappedBuffer(sendByte);
            context.WriteAndFlushAsync(sendBuffer).GetAwaiter().GetResult();
        }
        ReferenceCountUtil.SafeRelease(message);
    }

    public override void ChannelReadComplete(IChannelHandlerContext context)
    {
        context.Flush();
    }

    /// <summary>
    /// TBD
    /// </summary>
    /// <param name="context">TBD</param>
    /// <param name="exception">TBD</param>
    public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
    {
        var se = exception as SocketException;

        if (se?.SocketErrorCode == SocketError.OperationAborted)
        {
            //Logger().LogInformation($"Socket read operation aborted. Connection is about to be closed. Channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id})");
        }
        else if (se?.SocketErrorCode == SocketError.ConnectionReset)
        {
            //Logger().LogInformation($"Connection was reset by the remote peer. Channel [{context.Channel.LocalAddress}->{context.Channel.RemoteAddress}](Id={context.Channel.Id})");
        }
        else
        {
            base.ExceptionCaught(context, exception);
        }

        Console.WriteLine($"Exception: {exception.Message}");
        //Logger().LogError(exception, exception.Message);
        context.CloseAsync();
    }


    #endregion Overrides of ChannelHandlerAdapter
}

public class ThriftServer
{
    private TServer _server;
    private readonly IOptionsMonitor<ThriftServerOption> _thriftServerOption;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEnumerable<ITAsyncProcessor> _asyncProcessorList;
    public ThriftServer(//TServer server,
        IEnumerable<ITAsyncProcessor> asyncProcessorList,
        ILogger<ThriftServer> logger,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<ThriftServerOption> thriftServerOption)
    {
        _asyncProcessorList = asyncProcessorList;
        _thriftServerOption = thriftServerOption;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }


    public async Task Start(CancellationToken cancellationToken)
    {
        //_server.Start();

        //run the server

        //Task.Run(async () =>
        //{
        //    try
        //    {
        //        await _server.ServeAsync(cancellationToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger().LogError(ex);
        //        throw ex;
        //    }
        //});

        //await _server.ServeAsync(cancellationToken);
        await RunAsync(cancellationToken);
        _logger.LogInformation("Started the thrift server ...");
        Console.WriteLine("Started the thrift server ...");
    }

    public async Task Stop()
    {
        //unregiste from register center
        await Task.CompletedTask;
        _server.Stop();
        _logger.LogInformation("stoped the thrift server ...");
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var selectedTransport = GetTransport();
        var selectedBuffering = GetBuffering();
        var selectedProtocol = GetProtocol();
        var multiplex = GetMultiplex();

        if (selectedTransport == Transport.Http)
        {
            if (multiplex)
                throw new Exception("This semple code does not yet allow multiplex over http (although Thrift itself of course does)");

            var asyncProcessor = _asyncProcessorList.FirstOrDefault();
            await new HttpServerSample(asyncProcessor).Run(cancellationToken);
        }
        else
        {
            await RunSelectedConfigurationAsync(selectedTransport, selectedBuffering, selectedProtocol, multiplex, cancellationToken);
        }
    }


    private bool GetMultiplex()
    {
        var mplex = _thriftServerOption.CurrentValue.Multiplex;
        return mplex;
    }

    private Protocol GetProtocol()
    {
        var protocol = _thriftServerOption.CurrentValue.Protocol;
        if (string.IsNullOrEmpty(protocol))
            return Protocol.Binary;

        protocol = protocol.Substring(0, 1).ToUpperInvariant() + protocol.Substring(1).ToLowerInvariant();
        if (Enum.TryParse(protocol, true, out Protocol selectedProtocol))
            return selectedProtocol;
        else
            return Protocol.Binary;
    }

    private Buffering GetBuffering()
    {
        var buffering = _thriftServerOption.CurrentValue.Buffering;
        if (string.IsNullOrEmpty(buffering))
            return Buffering.None;

        buffering = buffering.Substring(0, 1).ToUpperInvariant() + buffering.Substring(1).ToLowerInvariant();
        if (Enum.TryParse<Buffering>(buffering, out var selectedBuffering))
            return selectedBuffering;
        else
            return Buffering.None;
    }

    private Transport GetTransport()
    {
        var transport = _thriftServerOption.CurrentValue.Transport;
        if (string.IsNullOrEmpty(transport))
            return Transport.Tcp;

        transport = transport.Substring(0, 1).ToUpperInvariant() + transport.Substring(1).ToLowerInvariant();
        if (Enum.TryParse(transport, true, out Transport selectedTransport))
            return selectedTransport;
        else
            return Transport.Tcp;
    }

    private async Task RunSelectedConfigurationAsync(Transport transport, Buffering buffering, Protocol protocol, bool multiplex, CancellationToken cancellationToken)
    {
        var port = _thriftServerOption.CurrentValue.Port;
        var configuration = _thriftServerOption.CurrentValue.Configuration;
        TServerTransport serverTransport = transport switch
        {
            Transport.Tcp => new TServerSocketTransport(port, configuration),
            Transport.NamedPipe => new TNamedPipeServerTransport(".test", configuration),//, NamedPipeClientFlags.None),
            Transport.TcpTls => new TTlsServerSocketTransport(9090, configuration, GetCertificate(), ClientCertValidator, LocalCertificateSelectionCallback),
            _ => throw new ArgumentException("unsupported value $transport", nameof(transport)),
        };

        TTransportFactory transportFactory = buffering switch
        {
            Buffering.Buffered => new TBufferedTransport.Factory(),
            Buffering.Framed => new TFramedTransport.Factory(),
            // layered transport(s) are optional
            Buffering.None => null,
            _ => throw new ArgumentException("unsupported value $buffering", nameof(buffering)),
        };

        TProtocolFactory protocolFactory = protocol switch
        {
            Protocol.Binary => new TBinaryProtocol.Factory(),
            Protocol.Compact => new TCompactProtocol.Factory(),
            Protocol.Json => new TJsonProtocol.Factory(),
            Protocol.BinaryHeader => new TBinaryHeaderServerProtocol.Factory(),
            Protocol.CompactHeader => new TCompactHeaderServerProtocol.Factory(),
            Protocol.JsonHeader => new TJsonHeaderServerProtocol.Factory(),
            _ => throw new ArgumentException("unsupported value $protocol", nameof(protocol)),
        };

        //var handler = new CalculatorAsyncHandler();
        //ITAsyncProcessor processor = new Calculator.AsyncProcessor(handler);
        ITAsyncProcessor processor = _asyncProcessorList.FirstOrDefault();
        if (multiplex)
        {
            var multiplexedProcessor = new TMultiplexedProcessor();
            foreach (var item in _asyncProcessorList)
            {
                multiplexedProcessor.RegisterProcessor(item.GetType().FullName, item);
            }

            processor = multiplexedProcessor;
        }


        try
        {
            _logger.LogInformation(
                string.Format(
                    "TSimpleAsyncServer with \n{0} transport\n{1} buffering\nmultiplex = {2}\n{3} protocol",
                    transport,
                    buffering,
                    multiplex ? "yes" : "no",
                    protocol
                    ));


            _server = new TSimpleAsyncServer(
                itProcessorFactory: new TSingletonProcessorFactory(processor),
                serverTransport: serverTransport,
                inputTransportFactory: transportFactory,
                outputTransportFactory: transportFactory,
                inputProtocolFactory: protocolFactory,
                outputProtocolFactory: protocolFactory,
                logger: _loggerFactory.CreateLogger<TSimpleAsyncServer>());

            //var threadConfig = new TThreadPoolAsyncServer.Configuration();
            //var server = new TThreadPoolAsyncServer(
            //     processorFactory: new TSingletonProcessorFactory(processor),
            //     serverTransport: serverTransport,
            //     inputTransportFactory: transportFactory,
            //     outputTransportFactory: transportFactory,
            //     inputProtocolFactory: protocolFactory,
            //     outputProtocolFactory: protocolFactory,
            //     threadConfig: threadConfig,
            //     logger: loggerFactory.CreateLogger<TThreadPoolAsyncServer>());

            _logger.LogInformation("Starting the server...");

            await _server.ServeAsync(cancellationToken);
        }
        catch (Exception x)
        {
            _logger.LogInformation(x.ToString());
        }
    }

    private X509Certificate2 GetCertificate()
    {
        // due to files location in net core better to take certs from top folder
        var certFile = GetCertPath(Directory.GetParent(Directory.GetCurrentDirectory()));
        return new X509Certificate2(certFile, "ThriftTest");
    }

    private string GetCertPath(DirectoryInfo di, int maxCount = 6)
    {
        var topDir = di;
        var certFile =
            topDir.EnumerateFiles("ThriftTest.pfx", SearchOption.AllDirectories)
                .FirstOrDefault();
        if (certFile == null)
        {
            if (maxCount == 0)
                throw new FileNotFoundException("Cannot find file in directories");
            return GetCertPath(di.Parent, maxCount - 1);
        }

        return certFile.FullName;
    }

    private X509Certificate LocalCertificateSelectionCallback(object sender,
        string targetHost, X509CertificateCollection localCertificates,
        X509Certificate remoteCertificate, string[] acceptableIssuers)
    {
        return GetCertificate();
    }

    private bool ClientCertValidator(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    private enum Transport
    {
        Tcp,
        NamedPipe,
        Http,
        TcpTls,
    }

    private enum Buffering
    {
        None,
        Buffered,
        Framed,
    }

    private enum Protocol
    {
        Binary,
        Compact,
        Json,
        BinaryHeader,
        CompactHeader,
        JsonHeader,
    }

    public class HttpServerSample
    {
        private readonly ITAsyncProcessor _asyncProcessor;
        public HttpServerSample(ITAsyncProcessor asyncProcessor)
        {
            _asyncProcessor = asyncProcessor;
        }
        public async Task Run(CancellationToken cancellationToken)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel()
                .UseUrls("http://localhost:9090")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(services => {
                    services.AddTransient(w => _asyncProcessor);
                })
                .UseStartup<Startup>()
                .ConfigureLogging((ctx, logging) => ConfigureLogging(logging))
                .Build();

            await host.RunAsync(cancellationToken);
        }

        private void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            logging.AddConsole();
            logging.AddDebug();
        }

        public class Startup
        {
            public Startup(Microsoft.Extensions.Hosting.IHostingEnvironment env)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(env.ContentRootPath)
                    .AddEnvironmentVariables();

                Configuration = builder.Build();
            }

            public IConfigurationRoot Configuration { get; }

            // This method gets called by the runtime. Use this method to add services to the container.
            public void ConfigureServices(IServiceCollection services)
            {
                // NOTE: this is not really the recommended way to do it
                // because the HTTP server cannot be configured properly to e.g. accept framed or multiplex

                //services.AddTransient<Calculator.IAsync, CalculatorAsyncHandler>();
                //services.AddTransient<ITAsyncProcessor, _asyncProcessor>();
                services.AddTransient<THttpServerTransport, THttpServerTransport>();
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app)//, IHostingEnvironment env, ILoggerFactory loggerFactory)
            {
                app.UseMiddleware<THttpServerTransport>();
            }
        }
    }

}

public class ThriftServerOption
{
    public string Transport { get; set; }
    public string Buffering { get; set; }
    public string Protocol { get; set; }
    public bool Multiplex { get; set; } = false;
    public int Port { get; set; }
    public TConfiguration Configuration { get; set; } = new TConfiguration();
    public int ClientTimeOut { get; set; }
}

public class HttpServer
{
    private readonly IWebHost _server;
    public HttpServer(IWebHost server)
    {
        _server = server;
    }


    public async Task Start(CancellationToken cancellationToken)
    {
        try
        {
            await _server.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
        }

    }

    public async Task Stop()
    {
        await _server.StopAsync();
        _server.Dispose();
    }
}