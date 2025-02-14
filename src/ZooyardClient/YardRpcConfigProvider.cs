﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Zooyard.Configuration;
using Zooyard.ConfigurationMapper;

namespace ZooyardClient;

internal class YardRpcConfigProvider : IRpcConfigProvider
{
    private readonly ILogger _logger;
    private readonly IYardConfigMapper _configMapper;
    private readonly IOptionsMonitor<ServiceOption> _serviceOption;
    private readonly IOptionsMonitor<ZooyardServiceOption> _zooyardServiceOption;
    private readonly IConfiguration _configuration;
    private volatile IRpcConfig? _snapshot;
    private CancellationTokenSource? _changeToken;

    public YardRpcConfigProvider(
        ILogger<YardRpcConfigProvider> logger,
        IOptionsMonitor<ServiceOption> serviceOption,
        IOptionsMonitor<ZooyardServiceOption> zooyardServiceOption,
        IConfiguration configuration,
        IYardConfigMapper configMapper)
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

        if (_snapshot == null) throw new YardRpcException("Can not get the zooyard config!!");

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
        YardRpcConfig? newConfig = null;
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

    public YardRpcConfig GetRealTimeConfig()
    {
        var contracts = new List<string>(_zooyardServiceOption.CurrentValue.Contracts);
        var metadata = new Dictionary<string, string>(_zooyardServiceOption.CurrentValue.Metadata);
        var services = new Dictionary<string, ServiceConfig>();

        foreach (var item in _zooyardServiceOption.CurrentValue.Services)
        {
            services[item.Key] = new ServiceConfig
            {
                ServiceId = string.IsNullOrWhiteSpace(item.Value.ServiceId) ? item.Key : item.Value.ServiceId,
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

        var snapshot = new YardRpcConfig(contracts, metadata, services);

        return snapshot;
    }
}
