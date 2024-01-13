using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using Zooyard.Utils;

namespace Zooyard.Configuration.ConfigProvider;

internal sealed class ConfigurationConfigProvider : IRpcConfigProvider, IDisposable
{
    private readonly object _lockObject = new();
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private ConfigurationSnapshot? _snapshot;
    private CancellationTokenSource? _changeToken;
    private bool _disposed;
    private IDisposable? _subscription;

    public ConfigurationConfigProvider(
        ILogger<ConfigurationConfigProvider> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _subscription?.Dispose();
            _changeToken?.Dispose();
            _disposed = true;
        }
    }

    public IRpcConfig GetConfig()
    {
        // First time load
        if (_snapshot is null)
        {
            _subscription = ChangeToken.OnChange(_configuration.GetReloadToken, UpdateSnapshot);
            UpdateSnapshot();
        }

        return _snapshot;
    }

    [MemberNotNull(nameof(_snapshot))]
    private void UpdateSnapshot()
    {
        // Prevent overlapping updates, especially on startup.
        lock (_lockObject)
        {
            Log.LoadData(_logger);
            ConfigurationSnapshot newSnapshot;
            try
            {
                var contracts = CreateContracts(_configuration.GetSection(nameof(ConfigurationSnapshot.Contracts)));
                var metadata = CreateMetadata(_configuration.GetSection(nameof(ConfigurationSnapshot.Metadata)));
                var services = CreateServices(_configuration.GetSection(nameof(ConfigurationSnapshot.Services)));
                newSnapshot = new ConfigurationSnapshot 
                {
                    Contracts = contracts,
                    Metadata = metadata,
                    Services = services,
                };
            }
            catch (Exception ex)
            {
                Log.ConfigurationDataConversionFailed(_logger, ex);

                // Re-throw on the first time load to prevent app from starting.
                if (_snapshot is null)
                {
                    throw;
                }

                return;
            }

            var oldToken = _changeToken;
            _changeToken = new CancellationTokenSource();
            newSnapshot.ChangeToken = new CancellationChangeToken(_changeToken.Token);
            _snapshot = newSnapshot;

            try
            {
                oldToken?.Cancel(throwOnFirstException: false);
            }
            catch (Exception ex)
            {
                Log.ErrorSignalingChange(_logger, ex);
            }
        }
    }

    private IReadOnlyList<string> CreateContracts(IConfigurationSection section)
    {
        var result = section.ReadStringArray();
        return result?? Array.Empty<string>();
    }

    private IReadOnlyDictionary<string, string> CreateMetadata(IConfigurationSection section)
    {
        var metadata = section.ReadStringDictionary();
        return metadata;
    }

    private IReadOnlyDictionary<string, ServiceConfig> CreateServices(IConfigurationSection section)
    {
        var services = new Dictionary<string, ServiceConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var service in section.GetChildren())
        {
            services.Add(service.Key, CreateService(service));
        }

        return services;
    }

    private static ServiceConfig CreateService(IConfigurationSection section)
    {
        var instances = new Dictionary<string, InstanceConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in section.GetSection(nameof(ServiceConfig.Instances)).GetChildren())
        {
            instances.Add(instance.Key, CreateInstance(instance));
        }

        return new ServiceConfig
        {
            ServiceName = section[nameof(ServiceConfig.ServiceName)]!,
            Metadata = section.GetSection(nameof(ServiceConfig.Metadata)).ReadStringDictionary(),
            Instances = instances
        };
    }

    private static InstanceConfig CreateInstance(IConfigurationSection section) 
    {
        return new InstanceConfig
        {
            Host = section[nameof(InstanceConfig.Host)]!,
            Port = section.GetValue(nameof(InstanceConfig.Port), 80),
            Metadata = section.GetSection(nameof(ServiceConfig.Metadata)).ReadStringDictionary(),
        };
    }


    private static class Log
    {
        private static readonly Action<ILogger, Exception> _errorSignalingChange = LoggerMessage.Define(
            Microsoft.Extensions.Logging.LogLevel.Error,
            EventIds.ErrorSignalingChange,
            "An exception was thrown from the change notification.");

        private static readonly Action<ILogger, Exception?> _loadData = LoggerMessage.Define(
            Microsoft.Extensions.Logging.LogLevel.Information,
            EventIds.LoadData,
            "Loading proxy data from config.");

        private static readonly Action<ILogger, Exception> _configurationDataConversionFailed = LoggerMessage.Define(
            Microsoft.Extensions.Logging.LogLevel.Error,
            EventIds.ConfigurationDataConversionFailed,
            "Configuration data conversion failed.");

        public static void ErrorSignalingChange(ILogger logger, Exception exception)
        {
            _errorSignalingChange(logger, exception);
        }

        public static void LoadData(ILogger logger)
        {
            _loadData(logger, null);
        }

        public static void ConfigurationDataConversionFailed(ILogger logger, Exception exception)
        {
            _configurationDataConversionFailed(logger, exception);
        }
    }
}
