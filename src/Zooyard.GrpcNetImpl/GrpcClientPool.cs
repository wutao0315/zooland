using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
//using Zooyard.Logging;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.GrpcNetImpl;

public class GrpcClientPool : AbstractClientPool
{
    public const string TIMEOUT_KEY = "timeout";
    public const int DEFAULT_TIMEOUT = 10000;
    public const string MAXLENGTH_KEY = "maxlength";
    public const int DEFAULT_MAXLENGTH = int.MaxValue;
    public const string CREDENTIALS_KEY = "credentials";
    public const string DEFAULT_CREDENTIALS = "Insecure";


    private readonly IDictionary<string, ChannelCredentials> _credentials;
    private readonly IEnumerable<ClientInterceptor> _interceptors;
    private readonly ILoggerFactory _loggerFactory;


    public GrpcClientPool(IDictionary<string, ChannelCredentials> credentials,
        IEnumerable<ClientInterceptor> interceptors,
        ILoggerFactory loggerFactory):base(loggerFactory.CreateLogger<GrpcClientPool>())
    {
        _credentials = credentials;
        _interceptors = interceptors;
        _loggerFactory = loggerFactory;
    }

    protected override async Task<IClient> CreateClient(URL url)
    {
        //实例化TheTransport
        //获得transport参数,用于反射实例化
        var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);

        if (base.ProxyType == null) 
        {
            throw new Zooyard.Rpc.RpcException("not find the proxy grpc client");
        }

       
        var maxReceiveMessageLength = url.GetParameter(MAXLENGTH_KEY, DEFAULT_MAXLENGTH);
      
        var grpcChannelOption = new GrpcChannelOptions
        {
            MaxReceiveMessageSize = maxReceiveMessageLength,
            LoggerFactory = _loggerFactory
        };

        var credentialsKey = url.GetParameter(CREDENTIALS_KEY, DEFAULT_CREDENTIALS);
        if (_credentials != null 
            && _credentials.ContainsKey(credentialsKey) 
            && credentialsKey!= DEFAULT_CREDENTIALS)
        {
            grpcChannelOption.Credentials =_credentials[credentialsKey];
        }

        var address = $"{url.Protocol}://{url.Host}:{url.Port}";
        var channel = GrpcChannel.ForAddress(address, grpcChannelOption);
        await Task.CompletedTask;

        object? client;

        if (_interceptors?.Count() > 0)
        {
            var callInvoker = channel.Intercept(_interceptors.ToArray());
            //实例化GrpcClient
            client = Activator.CreateInstance(ProxyType, callInvoker);
        }
        else {
            //实例化GrpcClient
            client = Activator.CreateInstance(ProxyType, channel);
        }
        if (client == null) 
        {
            throw new Exception($"grpc client is null");
        }

        return new GrpcClient(_loggerFactory.CreateLogger<GrpcClient>(), channel, client, url, grpcChannelOption, timeout);
    }
}
