using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooyard.DotNettyImpl;
using Zooyard.DotNettyImpl.Codec;

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
        services.AddTransient((serviceProvider) => 
        {
            var option = serviceProvider.GetRequiredService<IOptionsMonitor<Zooyard.DotNettyImpl.NettyOption>>();
            var encoder = serviceProvider.GetRequiredService<ITransportMessageEncoder>();
            var decoder = serviceProvider.GetRequiredService<ITransportMessageDecoder>();
            var looger = serviceProvider.GetRequiredService<ILogger<NettyClientPool>>();

            //var nettyProtocols = new Dictionary<string, NettyProtocol>();
            //foreach (var item in option.Protocols)
            //{
            //    var value = new NettyProtocol
            //    {
            //        EventLoopGroupType = Type.GetType(item.Value.EventLoopGroupType),
            //        ChannelType = Type.GetType(item.Value.ChannelType)
            //    };
            //    nettyProtocols.Add(item.Key, value);
            //}

            var pool = new NettyClientPool(encoder, decoder, option, looger);

            return pool;
        });
    }
}
