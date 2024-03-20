using Zooyard.Configuration;

namespace ZooyardClient;

public class DefaultYardConfigMapper : IYardConfigMapper
{

    public ServiceConfig CreateServiceConfig(string serviceName, NamingOption nameOption, IReadOnlyDictionary<string, InstanceConfig> instances)
    {
        return new ServiceConfig
        {
            ServiceName = serviceName,
            Metadata = nameOption.Metadata,
            Instances = instances,
        };
    }

    public IReadOnlyDictionary<string, InstanceConfig> CreateInstanceConfig(IReadOnlyDictionary<string, NamingInstanceOption> instances)
    {
        var result = new Dictionary<string, InstanceConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in instances)
        {
            var hostAndPort = item.Key.Split('#');
            var host = hostAndPort[0];
            var port = 80;
            int.TryParse(hostAndPort[1],out port);

            //var metadataPrefix = $"{ServiceExtensions.YardKey}:Instance:{nameof(InstanceConfig.Metadata)}:";
            //// filter the metadata from instance
            //var metadata = new ReadOnlyDictionary<string, string>(item.Value.Metadata
            //    .Where(x => x.Key.StartsWith(metadataPrefix, StringComparison.OrdinalIgnoreCase))
            //    .ToDictionary(s => s.Key.Replace(metadataPrefix,"", StringComparison.OrdinalIgnoreCase), s => s.Value, StringComparer.OrdinalIgnoreCase));

            var metadata = new Dictionary<string, string>(item.Value.Metadata);
            metadata[nameof(NamingInstanceOption.Ephemeral)] = item.Value.Ephemeral.ToString();
            metadata[nameof(NamingInstanceOption.Weight)] = item.Value.Weight.ToString("f2");

            var instance = new InstanceConfig
            {
                Host = host,
                Port = port,
                Metadata = metadata,
            };

            result.Add(item.Key, instance);
        }

        return result;
    }
}
