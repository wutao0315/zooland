using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Extensions.Hosting;
using NLog.Extensions.Logging;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Channels;
using Thrift;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;
using Thrift.Transport.Server;
using Zooyard;
using Zooyard.DotNettyImpl.Adapter;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.DotNettyImpl.Transport.Codec;
using Zooyard.GrpcImpl;
using Zooyard.HttpImpl;
using Zooyard.ThriftImpl.Header;

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

            services.Configure<GrpcServerOption>(config.GetSection("grpc"));
            services.Configure<NettyServerOption>(config.GetSection("netty"));
            services.Configure<ThriftServerOption>(config.GetSection("thrift"));
            services.AddLogging();


            services.AddTransient((serviceProvider) => "A");

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


            services.AddSingleton<TServer>(serviceProvider =>
            {
                var processor = serviceProvider.GetRequiredService<ITAsyncProcessor>();
                var serverTransport = serviceProvider.GetRequiredService<TServerTransport>();
                var transportFactory = serviceProvider.GetRequiredService<TTransportFactory>();
                var protocolFactory = serviceProvider.GetRequiredService<TProtocolFactory>();
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                var threadConfig = new TThreadPoolAsyncServer.Configuration();
                var server = new TThreadPoolAsyncServer(
                     processorFactory: new TSingletonProcessorFactory(processor),
                     serverTransport: serverTransport,
                     inputTransportFactory: transportFactory,
                     outputTransportFactory: transportFactory,
                     inputProtocolFactory: protocolFactory,
                     outputProtocolFactory: protocolFactory,
                     threadConfig: threadConfig,
                     logger: loggerFactory.CreateLogger<TThreadPoolAsyncServer>());
                return server;
            });

            //services.AddSingleton<TServer>(serviceProvider =>
            //{
            //    var processor = serviceProvider.GetRequiredService<ITAsyncProcessor>();
            //    var serverTransport = serviceProvider.GetRequiredService<TServerTransport>();
            //    var transportFactory = serviceProvider.GetRequiredService<TTransportFactory>();
            //    var protocolFactory = serviceProvider.GetRequiredService<TProtocolFactory>();
            //    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            //    return new TSimpleAsyncServer(
            //        itProcessorFactory: new TSingletonProcessorFactory(processor),
            //        serverTransport: serverTransport,
            //        inputTransportFactory: transportFactory,
            //        outputTransportFactory: transportFactory,
            //        inputProtocolFactory: protocolFactory,
            //        outputProtocolFactory: protocolFactory,
            //        logger: loggerFactory.CreateLogger<TSimpleAsyncServer>());
            //});

            services.AddHttpServer<Startup>(args);

            services.AddThriftServer();

            services.AddSingleton((p) => new GrpcNetServer(args));

            //services.AddZoolandServer();
            services.AddHostedService<ZoolandHostedService>();

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
        var metadata = new Metadata
        {
            new Metadata.Entry("test", "test")
        };
        var options = context.Options.WithHeaders(metadata);
        context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        var response = continuation(request, context);
        return response;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = new Metadata
        {
            new Metadata.Entry("test", "test")
        };
        var options = context.Options.WithHeaders(metadata);
        context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        var response = continuation(request, context);

        var responseAsync = response.ResponseAsync.ContinueWith<TResponse>((r) => r.Result);
        return new AsyncUnaryCall<TResponse>(responseAsync, response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);

    }
}

public abstract class ServerInterceptor : Interceptor
{
}
public class ServerGrpcInterceptor : ServerInterceptor
{
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = new Metadata
        {
            new Metadata.Entry("test", "test")
        };
        var options = context.Options.WithHeaders(metadata);
        context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        var response = continuation(request, context);
        return response;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = new Metadata
        {
            new Metadata.Entry("test", "test")
        };
        var options = context.Options.WithHeaders(metadata);
        context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        var response = continuation(request, context);

        var responseAsync = response.ResponseAsync.ContinueWith<TResponse>((r) => r.Result);
        return new AsyncUnaryCall<TResponse>(responseAsync, response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);

    }
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        context.Options.Headers?.Add(new Metadata.Entry("test", "test"));
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
public class GrpcServerOption
{
    public Dictionary<string, string> Services { get; set; } = new();
    public List<GrpcServerPortOption> ServerPorts { get; set; } = new();
}
public class GrpcServerPortOption
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Credentials { get; set; } = string.Empty;
}

public class NettyServerOption 
{
    public string ServiceType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

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
                var contractType = System.Type.GetType(item.Key)!;
                var implType = System.Type.GetType(item.Value)!;
                var implValue = serviceProvder.GetService(implType);
                var definition = (ServerServiceDefinition)contractType.GetMethod("BindService", new[] { implType })!
                .Invoke(null, new[] { implValue })!;
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
                    var credentialType = System.Type.GetType(item.Credentials)!;
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
        services.AddSingleton<ITransportMessageCodecFactory, JsonTransportMessageCodecFactory>(); 
        services.AddSingleton<DotNettyServerMessageListener>();
        services.AddSingleton<NettyServer>();
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
            var urls = "http://0.0.0.0:10010/";
            var host = new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>()
            .UseUrls(urls)
            .Build();
            Console.WriteLine(urls);
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
    private readonly GrpcNetServer _grpcNetServer;

    public ZoolandHostedService(GrpcServer grpcServer,
        ThriftServer thriftServer, 
        NettyServer nettyServer, 
        HttpServer httpServer,
        GrpcNetServer grpcNetServer)
    {
        _grpcServer = grpcServer;
        _thriftServer = thriftServer;
        _nettyServer = nettyServer;
        _httpServer = httpServer;
        _grpcNetServer = grpcNetServer;
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
            await _grpcNetServer.Start(cancellationToken).ConfigureAwait(false);
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
            await _grpcServer.Stop(cancellationToken).ConfigureAwait(false);
            await _thriftServer.Stop(cancellationToken).ConfigureAwait(false);
            await _nettyServer.Stop(cancellationToken).ConfigureAwait(false);
            await _httpServer.Stop(cancellationToken).ConfigureAwait(false);
            await _grpcNetServer.Stop(cancellationToken).ConfigureAwait(false);
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

    public async Task Stop(CancellationToken cancellationToken)
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
    private readonly IOptionsMonitor<NettyServerOption> _nettyServerOption;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DotNettyServerMessageListener _dotNettyServerMessageListener;
    public NettyServer(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory serviceScopeFactory,
        DotNettyServerMessageListener dotNettyServerMessageListener,
        IOptionsMonitor<NettyServerOption> nettyServerOption)
    {
        _loggerFactory = loggerFactory;
        _serviceScopeFactory = serviceScopeFactory;
        _dotNettyServerMessageListener = dotNettyServerMessageListener;
        _nettyServerOption = nettyServerOption;
    }
    
    public async Task Start(CancellationToken cancellationToken)
    {
        _dotNettyServerMessageListener.Received += async (sender, request) => 
        {
            if (request == null) 
            {
                throw new Exception("request invoke message is null");
            }
            var rpc = request.GetContent<RemoteInvokeMessage>();

            var methodName = rpc.Method;
            
            var types = rpc.ArgumentTypes;

            var arguments = new object[rpc.Arguments.Length];
            for (int i = 0; i < rpc.Arguments.Length; i++)
            {
                if (rpc.ArgumentTypes[i].IsValueType || rpc.ArgumentTypes[i] == typeof(string))
                {
                    arguments[i] = rpc.Arguments[i];
                }
                else 
                {
                    var arg = JsonConvert.SerializeObject(rpc.Arguments[i]);
                    arguments[i] = JsonConvert.DeserializeObject(arg, rpc.ArgumentTypes[i])!;
                }
            }
            //var types = (from item in arguments select item.GetType()).ToArray();
            var remoteInvoker = new RemoteInvokeResultMessage
            {
                Msg = "ok",
                Code = 0
            };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService(System.Type.GetType(_nettyServerOption.CurrentValue.ServiceType)!);
                var method = service.GetType().GetMethod(methodName, types);
                if (method == null)
                {
                    throw new Exception($"method {methodName} not exits");
                }
                var result = method.Invoke(service, arguments);

                if (method.ReturnType == typeof(Task) && result != null)
                {
                    await (Task)result;
                    remoteInvoker.Data = null;
                }
                else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) && result != null)
                {
                    var awaiter = result.GetType().GetMethod("GetAwaiter")!.Invoke(result, Array.Empty<object>())!;
                    var value = awaiter.GetType().GetMethod("GetResult")!.Invoke(awaiter, Array.Empty<object>())!;
                    remoteInvoker.Data = value;
                }
                else
                {
                    remoteInvoker.Data = result;
                }
            }
            catch (Exception ex)
            {
                remoteInvoker.Msg = ex.Message;
                remoteInvoker.Code = 500;
                //Logger().LogError(ex, ex.Message);
            }
            var response = TransportMessageExtensions.CreateInvokeResultMessage(request.Id, remoteInvoker);
            await sender.SendAndFlushAsync(response);
        };

        var url = URL.ValueOf(_nettyServerOption.CurrentValue.Url);
        await _dotNettyServerMessageListener.StartAsync(url);
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        try
        {
            _dotNettyServerMessageListener.CloseAsync();
            _dotNettyServerMessageListener.Dispose();
            await Task.CompletedTask;
        }
        finally
        {
        }
    }

    private class ServerHandler : ChannelHandlerAdapter
    {
        private readonly Action<IChannelHandlerContext, TransportMessage> _readAction;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public ServerHandler(Action<IChannelHandlerContext, TransportMessage> readAction, Microsoft.Extensions.Logging.ILogger logger)
        {
            _readAction = readAction;
            _logger = logger;
        }


        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var transportMessage = (TransportMessage)message;
            _readAction(context, transportMessage);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            context.CloseAsync();//客户端主动断开需要应答，否则socket变成CLOSE_WAIT状态导致socket资源耗尽
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
                _logger.LogError(exception, $"与服务器：{context.Channel.RemoteAddress}通信时发送了错误。");
        }
    }
}

public class DotNettyServerMessageListener : IMessageListener, IDisposable
{

    private readonly ILogger<DotNettyServerMessageListener> _logger;
    private readonly ITransportMessageDecoder _transportMessageDecoder;
    private readonly ITransportMessageEncoder _transportMessageEncoder;
    private DotNetty.Transport.Channels.IChannel? _channel;


    public DotNettyServerMessageListener(ILogger<DotNettyServerMessageListener> logger,
        ITransportMessageCodecFactory codecFactory
        )
    {
        _logger = logger;
        _transportMessageEncoder = codecFactory.GetEncoder();
        _transportMessageDecoder = codecFactory.GetDecoder();
    }


    public event ReceivedDelegate? Received;

    /// <summary>
    /// 触发接收到消息事件。
    /// </summary>
    /// <param name="sender">消息发送者。</param>
    /// <param name="message">接收到的消息。</param>
    public async Task OnReceived(IMessageSender sender, TransportMessage? message)
    {
        if (Received == null)
            return;
        await Received(sender, message);
    }


    public async Task StartAsync(URL url)
    {
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            _logger.LogDebug($"准备启动服务主机，监听地址：{url}。");

        Console.WriteLine($"准备启动服务主机，监听地址：{url}。");

        IEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
        IEventLoopGroup workerGroup = new MultithreadEventLoopGroup();//Default eventLoopCount is Environment.ProcessorCount * 2
        var bootstrap = new ServerBootstrap();
        var libuv = url.GetParameter("Libuv", false);
        if (libuv)
        {
            var dispatcher = new DispatcherEventLoopGroup();
            bossGroup = dispatcher;
            workerGroup = new WorkerEventLoopGroup(dispatcher);
            bootstrap.Channel<TcpServerChannel>();
        }
        else
        {
            bossGroup = new MultithreadEventLoopGroup(1);
            workerGroup = new MultithreadEventLoopGroup();
            bootstrap.Channel<TcpServerSocketChannel>();
        }
        var workerGroup1 = new SingleThreadEventLoop();
        bootstrap
        .Option(DotNetty.Transport.Channels.ChannelOption.SoBacklog, url.GetParameter("SoBacklog", 1024))
        .ChildOption(DotNetty.Transport.Channels.ChannelOption.Allocator, PooledByteBufferAllocator.Default)
        .Group(bossGroup, workerGroup)
        .ChildHandler(new ActionChannelInitializer<DotNetty.Transport.Channels.IChannel>(channel =>
        {
            var pipeline = channel.Pipeline;
            pipeline.AddLast(new LengthFieldPrepender(4));
            pipeline.AddLast(new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
            pipeline.AddLast(workerGroup1, "HandlerAdapter", new TransportMessageChannelHandlerAdapter(_transportMessageDecoder));
            pipeline.AddLast(workerGroup1, "ServerHandler", new ServerHandler(async (contenxt, message) =>
            {
                var sender = new DotNettyServerMessageSender(_transportMessageEncoder, contenxt);
                await OnReceived(sender, message);
            }, _logger));
        }));
        try
        {
            var endPoint = new IPEndPoint(IPAddress.Any, url.Port);
            _channel = await bootstrap.BindAsync(endPoint);
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                _logger.LogDebug($"服务主机启动成功，监听地址：{url}。");
        }
        catch
        {
            _logger.LogError($"服务主机启动失败，监听地址：{url}。 ");
        }
    }

    public void CloseAsync()
    {
        Task.Run(async () =>
        {
            if (_channel!=null) 
            {
                await _channel.EventLoop.ShutdownGracefullyAsync();
                await _channel.CloseAsync();
            }
        }).Wait();
    }


    public void Dispose()
    {
        Task.Run(async () =>
        {
            if (_channel != null)
            {
                await _channel.DisconnectAsync();
            }
        }).Wait();
    }

    private class ServerHandler : ChannelHandlerAdapter
    {
        private readonly Action<IChannelHandlerContext, TransportMessage> _readAction;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public ServerHandler(Action<IChannelHandlerContext, TransportMessage> readAction, Microsoft.Extensions.Logging.ILogger logger)
        {
            _readAction = readAction;
            _logger = logger;
        }


        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var transportMessage = (TransportMessage)message;
            _readAction(context, transportMessage);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            context.CloseAsync();//客户端主动断开需要应答，否则socket变成CLOSE_WAIT状态导致socket资源耗尽
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
                _logger.LogError(exception, $"与服务器：{context.Channel.RemoteAddress}通信时发送了错误。");
        }

    }
}

public class ThriftServer
{
    private TServer? _server;
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

        _ = Task.Run(async () =>
        {
            try
            {
                await RunAsync(cancellationToken);
            }
            catch (Exception)
            {
                throw;
            }
        }, cancellationToken);

        //await _server.ServeAsync(cancellationToken);
        
        _logger.LogInformation("Started the thrift server ...");
        Console.WriteLine("Started the thrift server ...");
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        //unregiste from register center
        await Task.CompletedTask;
        _server?.Stop();
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
        if (System.Enum.TryParse(protocol, true, out Protocol selectedProtocol))
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
        if (System.Enum.TryParse<Buffering>(buffering, out var selectedBuffering))
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
        if (System.Enum.TryParse(transport, true, out Transport selectedTransport))
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
            Transport.NamedPipe => new TNamedPipeServerTransport(".test", configuration, NamedPipeClientFlags.None),
            Transport.TcpTls => new TTlsServerSocketTransport(9090, configuration, GetCertificate(), ClientCertValidator, LocalCertificateSelectionCallback),
            _ => throw new ArgumentException("unsupported value $transport", nameof(transport)),
        };

        TTransportFactory? transportFactory = buffering switch
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


            //_server = new TSimpleAsyncServer(
            //    itProcessorFactory: new TSingletonProcessorFactory(processor),
            //    serverTransport: serverTransport,
            //    inputTransportFactory: transportFactory,
            //    outputTransportFactory: transportFactory,
            //    inputProtocolFactory: protocolFactory,
            //    outputProtocolFactory: protocolFactory,
            //    logger: _loggerFactory.CreateLogger<TSimpleAsyncServer>());

            var threadConfig = new TThreadPoolAsyncServer.Configuration();
            _server = new TThreadPoolAsyncServer(
                 processorFactory: new TSingletonProcessorFactory(processor),
                 serverTransport: serverTransport,
                 inputTransportFactory: transportFactory,
                 outputTransportFactory: transportFactory,
                 inputProtocolFactory: protocolFactory,
                 outputProtocolFactory: protocolFactory,
                 threadConfig: threadConfig,
                 logger: _loggerFactory.CreateLogger<TThreadPoolAsyncServer>());

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
        string targetHost, X509CertificateCollection? localCertificates,
        X509Certificate? remoteCertificate, string[] acceptableIssuers)
    {
        return GetCertificate();
    }

    private bool ClientCertValidator(object sender, X509Certificate? certificate,
        X509Chain? chain, SslPolicyErrors sslPolicyErrors)
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
    public string Transport { get; set; } = String.Empty;
    public string Buffering { get; set; } = String.Empty;
    public string Protocol { get; set; } = String.Empty;
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
        _ = Task.Run(async () =>
        {
            try
            {
                await _server.RunAsync(cancellationToken);
            }
            catch (Exception)
            {
            }
        }, cancellationToken);

    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        await _server.StopAsync(cancellationToken);
        _server.Dispose();
    }
}

public class GrpcNetServer
{
    private readonly WebApplication _app;
    public GrpcNetServer(string[] args) 
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options =>
        {
            Console.WriteLine("grpc:10011");
            options.Listen(IPAddress.Any, 10011, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });
        builder.Services.AddScoped<IHelloRepository, HelloRepository>();
        builder.Services.AddGrpc();
        _app = builder.Build();
        _app.MapGrpcService<HelloServiceGrpcNetImpl>();
    }
    

    public async Task Start(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _app.RunAsync(cancellationToken);
            }
            catch (Exception)
            {
            }
        }, cancellationToken);
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        await _app.StopAsync(cancellationToken);
    }
}