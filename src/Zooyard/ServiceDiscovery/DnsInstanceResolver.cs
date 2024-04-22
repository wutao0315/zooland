using Zooyard.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace Zooyard.ServiceDiscovery;

/// <summary>
/// Implementation of <see cref="IInstanceResolver"/> which resolves host names to IP addresses using DNS.
/// </summary>
internal class DnsInstanceResolver : IInstanceResolver
{
    private readonly IOptionsMonitor<DnsInstanceResolverOptions> _options;

    /// <summary>
    /// Initializes a new <see cref="DnsInstanceResolver"/> instance.
    /// </summary>
    /// <param name="options">The options.</param>
    public DnsInstanceResolver(IOptionsMonitor<DnsInstanceResolverOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public async ValueTask<ResolvedInstanceCollection> ResolveInstancesAsync(IDictionary<string, InstanceConfig> instances, CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        Dictionary<string, InstanceConfig> results = new();
        var tasks = new List<Task<List<(string Name, InstanceConfig Config)>>>(instances.Count);
        foreach (var (instanceId, instanceConfig) in instances)
        {
            tasks.Add(ResolveHostAsync(options, instanceId, instanceConfig, cancellationToken));
        }

        await Task.WhenAll(tasks);
        foreach (var task in tasks)
        {
            var configs = await task;
            foreach (var (name, config) in configs)
            {
                results[name] = config;
            }
        }

        var changeToken = options.RefreshPeriod switch
        {
            { } refreshPeriod when refreshPeriod > TimeSpan.Zero => new CancellationChangeToken(new CancellationTokenSource(refreshPeriod).Token),
            _ => null,
        };

        return new ResolvedInstanceCollection(results, changeToken);
    }

    private static async Task<List<(string Name, InstanceConfig Config)>> ResolveHostAsync(
        DnsInstanceResolverOptions options,
        string originalName,
        InstanceConfig originalConfig,
        CancellationToken cancellationToken)
    {
        var originalUri = new Uri($"http://{originalConfig.Host}:{originalConfig.Port}");
        var originalHost = originalConfig.Host is { Length: > 0 } host ? host : originalUri.Authority;
        var hostName = originalUri.DnsSafeHost;
        IPAddress[] addresses;
        try
        {
            addresses = options.AddressFamily switch
            {
                { } addressFamily => await Dns.GetHostAddressesAsync(hostName, addressFamily, cancellationToken).ConfigureAwait(false),
                null => await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(false)
            };
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to resolve host '{hostName}'. See {nameof(Exception.InnerException)} for details.", exception);
        }

        var results = new List<(string Name, InstanceConfig Config)>(addresses.Length);
        var uriBuilder = new UriBuilder(originalUri);
        //var healthUri = originalConfig.Health is { Length: > 0 } health ? new Uri(health) : null;
        //var healthUriBuilder = healthUri is { } ? new UriBuilder(healthUri) : null;
        foreach (var address in addresses)
        {
            var addressString = address.ToString();
            uriBuilder.Host = addressString;
            var resolvedAddress = uriBuilder.Uri.ToString();
            //var healthAddress = originalConfig.Health;
            //if (healthUriBuilder is not null)
            //{
            //    healthUriBuilder.Host = addressString;
            //    healthAddress = healthUriBuilder.Uri.ToString();
            //}

            var name = $"{originalName}[{addressString}]";
            var config = originalConfig with { 
                Host = originalHost,
                Port = uriBuilder.Port
                //Address = resolvedAddress, 
                //Health = healthAddress
            };
            results.Add((name, config));
        }

        return results;
    }
}
