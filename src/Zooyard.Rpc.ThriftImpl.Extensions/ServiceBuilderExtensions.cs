using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zooyard.Rpc.ThriftImpl.Extensions;

public class ThriftOption
{
    public IDictionary<string,string> Clients { get; set; }
}

public static class ServiceBuilderExtensions
{
    public static void AddThriftClient(this IServiceCollection services)
    {
        services.AddSingleton((serviceProvder) => 
        {
            var option = serviceProvder.GetService<IOptionsMonitor<ThriftOption>>().CurrentValue;
            var loggerFactory = serviceProvder.GetService<ILoggerFactory>();
            var thriftClientTypes = new Dictionary<string, Type>();
            foreach (var item in option.Clients)
            {
                thriftClientTypes.Add(item.Key, Type.GetType(item.Value));
            }

            var pool = new ThriftClientPool(clientTypes: thriftClientTypes
            );

            return pool;
        });
    }

    public static void AddThriftServer(this IServiceCollection services)
    {
        services.AddSingleton<IServer, ThriftServer>();
    }
}
