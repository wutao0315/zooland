using Microsoft.Extensions.DependencyInjection;
using Zooyard.ThriftImpl;

namespace Microsoft.Extensions.Configuration;

public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddThrift(this IRpcBuilder builder)
    {
        builder.Services.AddTransient<ThriftClientPool>();
        return builder;
    }
}
