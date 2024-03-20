using Zooyard.Configuration;

namespace Zooyard.ConfigurationMapper;

public interface IRpcConfigMapper
{
    ServiceConfig CreateServiceConfig(string serviceName, NamingOption namingOption, IReadOnlyDictionary<string, InstanceConfig> instances);

    IReadOnlyDictionary<string, InstanceConfig> CreateInstanceConfig(IReadOnlyDictionary<string, NamingInstanceOption> instances);
}
