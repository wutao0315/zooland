using Microsoft.Extensions.DependencyInjection;
using Zooyard.HttpImpl;

namespace Microsoft.Extensions.Configuration;

public static class ServiceBuilderExtensions
{
    public static void AddZooyardHttp(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddTransient<HttpClientPool>();
    }
}
