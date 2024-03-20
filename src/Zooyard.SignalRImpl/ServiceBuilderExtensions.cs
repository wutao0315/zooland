using Microsoft.Extensions.DependencyInjection;
using Zooyard.SignalRImpl;

namespace Microsoft.Extensions.Configuration;
public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddSignalR(this IRpcBuilder builder)
    {
        builder.Services.AddTransient<SignalRClientPool>();
        return builder;
    }
}
