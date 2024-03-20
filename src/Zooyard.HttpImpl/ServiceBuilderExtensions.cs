using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Zooyard.HttpImpl;

namespace Microsoft.Extensions.Configuration;

public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddHttp(this IRpcBuilder builder)
    {
        builder.Services.ConfigureHttpClientDefaults((b) => b.UseSocketsHttpHandler((s, p) => s.ActivityHeadersPropagator = DistributedContextPropagator.CreateDefaultPropagator()));
        builder.Services.AddHttpClient();
        builder.Services.AddTransient<HttpClientPool>();
        return builder;
    }
}
