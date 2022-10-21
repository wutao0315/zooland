using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooyard;
using Zooyard.ThriftImpl;

namespace Microsoft.Extensions.Configuration;

//public class ThriftOption
//{
//    public IDictionary<string,string> Clients { get; set; } = new Dictionary<string,string>();
//}

public static class ServiceBuilderExtensions
{
    public static void AddZooyardThrift(this IServiceCollection services)
    {
        services.AddTransient((serviceProvder) => 
        {
            //var option = serviceProvder.GetRequiredService<IOptionsMonitor<ThriftOption>>().CurrentValue;
            //var loggerFactory = serviceProvder.GetService<ILoggerFactory>();
            //var thriftClientTypes = new Dictionary<string, Type>();
            //foreach (var item in option.Clients)
            //{
            //    thriftClientTypes.Add(item.Key, Type.GetType(item.Value)!);
            //}

            var pool = new ThriftClientPool();

            return pool;
        });
    }
}
