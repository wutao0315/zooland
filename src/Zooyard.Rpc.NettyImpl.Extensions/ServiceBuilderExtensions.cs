using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Zooyard.Rpc.NettyImpl.Extensions;

public class NettyOption
{
    public IDictionary<string, NettyPortocolOption> Protocols { get; set; }
}
public class NettyPortocolOption
{
    public string EventLoopGroupType { get; set; }
    public string ChannelType { get; set; }
}

public class NettyServerOption
{
    public string ServiceType { get; set; }
    public string Url { get; set; }
    public bool IsSsl { get; set; } = false;
    public string Pfx { get; set; } = "";
    public string Pwd { get; set; } = "";
}


public static class ServiceBuilderExtensions
{
    public static void AddNettyClient(this IServiceCollection services)
    {
        services.AddSingleton((serviceProvider) => 
        {
            var option = serviceProvider.GetService<IOptionsMonitor<NettyOption>>().CurrentValue;

            var nettyProtocols = new Dictionary<string, NettyProtocol>();
            foreach (var item in option.Protocols)
            {
                var value = new NettyProtocol
                {
                    EventLoopGroupType = Type.GetType(item.Value.EventLoopGroupType),
                    ChannelType = Type.GetType(item.Value.ChannelType)
                };
                nettyProtocols.Add(item.Key, value);
            }
            
            var pool = new NettyClientPool(nettyProtocols: nettyProtocols);

            return pool;
        });
    }

    public static void AddNettyServer(this IServiceCollection services)
    {
        services.AddTransient<IServer>((serviceProvider)=> 
        {
            var option = serviceProvider.GetService<IOptionsMonitor<NettyServerOption>>().CurrentValue;
            var registryService = serviceProvider.GetService<IRegistryService>();
            var url = URL.ValueOf(option.Url);

            var service = serviceProvider.GetService(Type.GetType(option.ServiceType));
            
            return new NettyServer(url, service,  option.IsSsl, option.Pfx, option.Pwd, registryService);
        });
    }
}
