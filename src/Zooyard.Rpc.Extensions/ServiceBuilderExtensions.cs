using Microsoft.Extensions.DependencyInjection;

namespace Zooyard.Rpc.Extensions;

public static class ServiceBuilderExtensions
{
    public static void AddRpc(this IServiceCollection services)
    {
        services.AddHostedService<ZoolandHostedService>();
    }
}
