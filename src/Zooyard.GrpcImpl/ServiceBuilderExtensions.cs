using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zooyard.GrpcImpl;

namespace Microsoft.Extensions.Configuration;

public class GrpcOption
{
    public Dictionary<string, string> Credentials { get; set; } = new();
    //public Dictionary<string, string> Clients { get; set; } = new();
}

public class GrpcServerOption
{
    public Dictionary<string,string> Services { get; set; } = new();
    public List<ServerPortOption> ServerPorts { get; set; } = new();
}
public class ServerPortOption
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Credentials { get; set; } = string.Empty;
}

public static class ServiceBuilderExtensions
{
    public static void AddZooyardGrpc(this IServiceCollection services)
    {
        services.AddTransient((serviceProvder) => 
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
                    var credential = (ChannelCredentials)serviceProvder.GetRequiredService(Type.GetType(item.Value)!);
                    credentials.Add(item.Key, credential);
                }
            }

            //var grpcClientTypes = new Dictionary<string, Type>();
            //foreach (var item in option.Clients)
            //{
            //    grpcClientTypes.Add(item.Key, Type.GetType(item.Value)!);
            //}

            var interceptors = serviceProvder.GetServices<ClientInterceptor>();

            var pool = new GrpcClientPool(
                credentials:credentials,
                interceptors: interceptors
            );

            return pool;
        });

    }
}
