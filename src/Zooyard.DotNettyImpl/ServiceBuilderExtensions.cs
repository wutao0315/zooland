using Microsoft.Extensions.DependencyInjection;
using Zooyard.DotNettyImpl;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.DotNettyImpl.Transport.Codec;

namespace Microsoft.Extensions.Configuration;
public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddDotNetty(this IRpcBuilder builder)
    {
        builder.Services.AddSingleton<ITransportMessageCodecFactory, JsonTransportMessageCodecFactory>();
        builder.Services.AddTransient<NettyClientPool>();
        return builder;
    }
}
