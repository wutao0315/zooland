using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Zooyard.Configuration;

namespace Zooyard.ConfigurationMapper;

internal class DefaultRpcConfigProvider : IRpcConfigProvider
{
    private readonly ILogger _logger;
    private readonly IRpcConfigMapper _configMapper;
    private readonly IOptionsMonitor<ServiceOption> _serviceOption;
    private readonly IOptionsMonitor<ZooyardServiceOption> _zooyardServiceOption;
    private readonly IConfiguration _configuration;
    private volatile IRpcConfig? _snapshot;
    private CancellationTokenSource? _changeToken;

    public DefaultRpcConfigProvider(
        ILogger<DefaultRpcConfigProvider> logger,
        IOptionsMonitor<ServiceOption> serviceOption,
        IOptionsMonitor<ZooyardServiceOption> zooyardServiceOption,
        IConfiguration configuration,
        IRpcConfigMapper configMapper)
    {
        _logger = logger;
        _serviceOption = serviceOption;
        _serviceOption.OnChange(OnServiceChanged);
        _zooyardServiceOption = zooyardServiceOption;
        _zooyardServiceOption.OnChange(OnProxyChanged);
        _configuration = configuration;
        _configMapper = configMapper;
    }

    public IRpcConfig GetConfig()
    {
        if (_snapshot == null) 
        {
            UpdateConfig();
        }

        if (_snapshot == null) throw new Exception("Can not get the zooyard config!!");

        return _snapshot;
    }

    private void OnServiceChanged(ServiceOption value, string? name)
    {
        UpdateConfig();
    }
    private void OnProxyChanged(ZooyardServiceOption value, string? name)
    {
        UpdateConfig();
    }

    public void UpdateConfig() 
    {
        DefaultRpcConfig? newConfig = null;
        try
        {
            newConfig = GetRealTimeConfig();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Can not update yarp configuration.");

            if (_snapshot == null) throw;

            return;
        }

        var oldToken = _changeToken;
        _changeToken = new CancellationTokenSource();
        newConfig.ChangeToken = new CancellationChangeToken(_changeToken.Token);
        _snapshot = newConfig;

        try
        {
            oldToken?.Cancel(throwOnFirstException: false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Cancel old token error");
        }
    }

    public DefaultRpcConfig GetRealTimeConfig()
    {
        var contracts = new List<string>(_zooyardServiceOption.CurrentValue.Contracts);
        var metadata = new Dictionary<string, string>(_zooyardServiceOption.CurrentValue.Metadata);
        var services = new Dictionary<string, ServiceConfig>();

        foreach (var item in _zooyardServiceOption.CurrentValue.Services)
        {
            services[item.Key] = new ServiceConfig
            {
                ServiceName = string.IsNullOrWhiteSpace(item.Value.ServiceName) ? item.Key : item.Value.ServiceName,
                Instances = item.Value.Instances,
                Metadata = item.Value.Metadata,
            };
        }

        foreach (var item in _serviceOption.CurrentValue.Services)
        {
            // ServiceConfig
            var service = _configMapper.CreateServiceConfig(item.Key, item.Value, _configMapper.CreateInstanceConfig(item.Value.Instances));
            services[item.Key] = service;
        }

        var snapshot = new DefaultRpcConfig(contracts, metadata, services);

        return snapshot;
    }
}
