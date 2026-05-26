using Microsoft.Extensions.DependencyInjection;
using Zooyard.DotNettyImpl;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.DotNettyImpl.Transport.Codec;
using Zooyard.Rpc;

namespace Microsoft.Extensions.Configuration;
public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddDotNetty(this IRpcBuilder builder)
    {
        builder.Services.AddSingleton<ITransportMessageCodecFactory, JsonTransportMessageCodecFactory>();
        builder.Services.AddSingleton<NettyClientPool>();
        return builder;
    }
}
