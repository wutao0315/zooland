using Zooyard.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InMemoryConfigProviderExtensions
{
    /// <summary>
    /// Adds an InMemoryConfigProvider
    /// </summary>
    public static IRpcBuilder LoadFromMemory(this IRpcBuilder builder, IReadOnlyList<RouteConfig> routes, IReadOnlyList<ServiceConfig> clusters)
    {
        builder.Services.AddSingleton(new InMemoryConfigProvider(routes, clusters));
        builder.Services.AddSingleton<IRpcConfigProvider>(s => s.GetRequiredService<InMemoryConfigProvider>());
        return builder;
    }
}
