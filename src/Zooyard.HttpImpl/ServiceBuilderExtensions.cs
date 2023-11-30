using Microsoft.Extensions.DependencyInjection;
using Zooyard.HttpImpl;

namespace Microsoft.Extensions.Configuration;

public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddHttp(this IRpcBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddTransient<HttpClientPool>();
        return builder;
    }
}
