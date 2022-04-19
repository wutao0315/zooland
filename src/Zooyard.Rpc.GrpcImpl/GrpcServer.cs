using Grpc.Core;
using Grpc.Core.Interceptors;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl;

public class GrpcServer : AbstractServer
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(GrpcServer));
    private Server _server;
    public GrpcServer(IEnumerable<ServerServiceDefinition> services,
        IEnumerable<ServerPort> ports,
        IEnumerable<ServerInterceptor> interceptors,
        IRegistryService registryService) 
        : base(registryService)
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


    public override async Task DoExport(CancellationToken cancellationToken)
    {
        //开启服务
        _server.Start();
        await Task.CompletedTask;
        var ports = _server.Ports.Select(w=>w.Port);
        Logger().LogInformation($"Started the grpc server on{string.Join(",",ports)} ...");
    }

    public override async Task DoDispose()
    {
        //向注册中心发送注销请求
        if (_server != null)
        {
            try
            {
                await _server.ShutdownAsync();
                _server = null;
            }
            catch (Exception ex)
            {
                Logger().LogInformation(ex, ex.ToString());
            }
        }
    }
}
