using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooyard.GrpcNetImpl;

namespace Microsoft.Extensions.Configuration;

public class GrpcNetOption
{
    public Dictionary<string, string> Credentials { get; set; } = new();
}

public static class ServiceBuilderExtensions
{
    public static void AddZooyardGrpcNet(this IServiceCollection services)
    {
        services.AddSingleton<ClientInterceptor, ClientGrpcHeaderInterceptor>();
        
        services.AddTransient((serviceProvder) => 
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
                credentials:credentials,
                interceptors: interceptors,
                 loggerFactory: loggerFactory
            );

            return pool;
        });

    }
}
