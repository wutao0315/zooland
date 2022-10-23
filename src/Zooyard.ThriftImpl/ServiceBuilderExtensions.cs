using Microsoft.Extensions.DependencyInjection;
using Zooyard.ThriftImpl;

namespace Microsoft.Extensions.Configuration;

public static class ServiceBuilderExtensions
{
    public static void AddZooyardThrift(this IServiceCollection services)
    {
        services.AddTransient((serviceProvder) => 
        {
            var pool = new ThriftClientPool();

            return pool;
        });
    }
}
