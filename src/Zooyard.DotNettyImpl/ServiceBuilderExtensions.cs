using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zooyard.DotNettyImpl;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.DotNettyImpl.Transport.Codec;

namespace Microsoft.Extensions.Configuration;

//public class NettyOption
//{
//    public IDictionary<string, NettyPortocolOption> Protocols { get; set; }
//}
public class NettyPortocolOption
{
    public string EventLoopGroupType { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty;
}

public class NettyServerOption
{
    public string ServiceType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsSsl { get; set; } = false;
    public string Pfx { get; set; } = string.Empty;
    public string Pwd { get; set; } = string.Empty;
}


public static class ServiceBuilderExtensions
{
    public static void AddZooyardNetty(this IServiceCollection services)
    {
        services.AddSingleton<ITransportMessageCodecFactory, JsonTransportMessageCodecFactory>();
        services.AddTransient<NettyClientPool>();
    }
}
