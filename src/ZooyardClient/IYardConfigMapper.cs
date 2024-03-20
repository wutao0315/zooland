using Zooyard.Configuration;

namespace ZooyardClient;

public interface IYardConfigMapper
{
    //IReadOnlyDictionary<string, string> CreateMetadata(IConfiguration configuration);

    ServiceConfig CreateServiceConfig(string serviceName, NamingOption namingOption, IReadOnlyDictionary<string, InstanceConfig> instances);

    IReadOnlyDictionary<string, InstanceConfig> CreateInstanceConfig(IReadOnlyDictionary<string, NamingInstanceOption> instances);
}
