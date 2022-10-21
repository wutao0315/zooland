using Microsoft.Extensions.DependencyInjection;
using Zooyard.HttpImpl;

namespace Microsoft.Extensions.Configuration;

public class HttpServerOption
{
    public IEnumerable<string>  Urls { get; set; }
}

public static class ServiceBuilderExtensions
{
    public static void AddZooyardHttp(this IServiceCollection services)
    {
        services.AddTransient<HttpClientPool>();
    }
}
