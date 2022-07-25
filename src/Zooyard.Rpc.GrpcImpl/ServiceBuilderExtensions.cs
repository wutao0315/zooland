using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zooyard.Rpc.GrpcImpl;

namespace Microsoft.Extensions.Configuration;

public class GrpcOption
{
    public IDictionary<string, string> Credentials { get; set; }
    public IDictionary<string, string> Clients { get; set; }
}

public class GrpcServerOption
{
    public IDictionary<string,string> Services { get; set; }
    public IEnumerable<ServerPortOption> ServerPorts { get; set; }
}
public class ServerPortOption
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Credentials { get; set; } = string.Empty;
}

public static class ServiceBuilderExtensions
{
    public static void AddGrpcImpl(this IServiceCollection services)
    {
        services.AddSingleton((serviceProvder) => 
        {
            var option = serviceProvder.GetRequiredService<IOptionsMonitor<GrpcOption>>().CurrentValue;

            var credentials = new Dictionary<string, ChannelCredentials>
            {
                { "Insecure", ChannelCredentials.Insecure}
            };

            if (option.Credentials?.Count>0)
            {
                foreach (var item in option.Credentials)
                {
                    var credential = serviceProvder.GetRequiredService(Type.GetType(item.Value)) as ChannelCredentials;
                    credentials.Add(item.Key, credential);
                }
            }

            var grpcClientTypes = new Dictionary<string, Type>();
            foreach (var item in option.Clients)
            {
                grpcClientTypes.Add(item.Key, Type.GetType(item.Value));
            }

            var interceptors = serviceProvder.GetServices<ClientInterceptor>();

            var pool = new GrpcClientPool(
                credentials:credentials,
                grpcClientTypes:grpcClientTypes, 
                interceptors: interceptors
            );

            return pool;
        });

    }
}
