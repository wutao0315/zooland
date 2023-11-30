using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooyard.GrpcNetImpl;

namespace Microsoft.Extensions.Configuration;

public sealed record GrpcNetOption
{
    public Dictionary<string, string> Credentials { get; set; } = new();
}

public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddGrpcNet(this IRpcBuilder builder)
    {
        builder.Services.AddSingleton<ClientInterceptor, ClientGrpcHeaderInterceptor>();

        builder.Services.AddTransient((serviceProvder) => 
        {
            var option = serviceProvder.GetRequiredService<IOptionsMonitor<GrpcNetOption>>().CurrentValue;
            var loggerFactory = serviceProvder.GetRequiredService<ILoggerFactory>();

            var credentials = new Dictionary<string, ChannelCredentials>
            {
                { "Insecure", ChannelCredentials.Insecure}
            };

            if (option.Credentials?.Count>0)
            {
                foreach (var item in option.Credentials)
                {
                    var credential = (ChannelCredentials)serviceProvder.GetRequiredService(Type.GetType(item.Value)!);
                    credentials.Add(item.Key, credential);
                }
            }

            var interceptors = serviceProvder.GetServices<ClientInterceptor>();

            var pool = new GrpcClientPool(
                _credentials:credentials,
                _interceptors: interceptors,
                 _loggerFactory: loggerFactory
            );

            return pool;
        });

        return builder;
    }

}
