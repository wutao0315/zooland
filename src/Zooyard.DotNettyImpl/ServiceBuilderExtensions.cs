using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zooyard.DotNettyImpl;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.DotNettyImpl.Transport.Codec;

namespace Microsoft.Extensions.Configuration;

//public sealed record NettyPortocolOption
//{
//    public string EventLoopGroupType { get; set; } = string.Empty;
//    public string ChannelType { get; set; } = string.Empty;
//}

//public sealed record NettyServerOption
//{
//    public string ServiceType { get; set; } = string.Empty;
//    public string Url { get; set; } = string.Empty;
//    public bool IsSsl { get; set; } = false;
//    public string Pfx { get; set; } = string.Empty;
//    public string Pwd { get; set; } = string.Empty;
//}


public static class ServiceBuilderExtensions
{
    public static IRpcBuilder AddDotNetty(this IRpcBuilder builder)
    {
        builder.Services.AddSingleton<ITransportMessageCodecFactory, JsonTransportMessageCodecFactory>();
        builder.Services.AddTransient<NettyClientPool>();
        return builder;
    }
}
