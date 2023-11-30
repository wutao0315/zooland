using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Zooyard.Configuration;
using Zooyard.Model;
using Zooyard.Rpc;
using Zooyard.ServiceDiscovery;
using Zooyard.Utils;

namespace Zooyard.Management;

/// <summary>
/// Provides a method to apply Proxy configuration changes.
/// in a thread-safe manner while avoiding locks on the hot path.
/// </summary>
internal sealed class RpcConfigManager : IRpcStateLookup, IDisposable
{
    private static readonly IReadOnlyDictionary<string, ServiceConfig> _emptyServiceDictionary = new ReadOnlyDictionary<string, ServiceConfig>(new Dictionary<string, ServiceConfig>());
    private readonly object _syncRoot = new();
    private readonly ILogger<RpcConfigManager> _logger;
    private readonly IRpcConfigProvider[] _providers;
    private readonly ConfigState[] _configs;
    //private readonly IServiceChangeListener[] _serviceChangeListeners;

    private readonly ConcurrentStack<string> _contracts = new();
    private readonly ConcurrentDictionary<string, string> _metadata = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ServiceState> _services = new(StringComparer.OrdinalIgnoreCase);
    

    private readonly IRpcConfigFilter[] _filters;
    private readonly IConfigValidator _configValidator;
    //private readonly IClusterDestinationsUpdater _clusterDestinationsUpdater;
    private readonly IInstanceResolver _instanceResolver;
    private readonly IConfigChangeListener[] _configChangeListeners;
    private CancellationTokenSource _endpointsChangeSource = new();
    private IChangeToken _endpointsChangeToken;

    private CancellationTokenSource _configChangeSource = new();

    public RpcConfigManager(
        ILogger<RpcConfigManager> logger,
        IEnumerable<IRpcConfigProvider> providers,
        IEnumerable<IRpcConfigFilter> filters,
        IConfigValidator configValidator,
        //IClusterDestinationsUpdater clusterDestinationsUpdater,
        IEnumerable<IConfigChangeListener> configChangeListeners,
        IInstanceResolver instanceResolver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = providers?.ToArray() ?? throw new ArgumentNullException(nameof(providers));
        //_serviceChangeListeners = (serviceChangeListeners as IServiceChangeListener[])
            //?? serviceChangeListeners?.ToArray() ?? throw new ArgumentNullException(nameof(serviceChangeListeners));
        _filters = (filters as IRpcConfigFilter[]) ?? filters?.ToArray() ?? throw new ArgumentNullException(nameof(filters));
        _configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
        //_clusterDestinationsUpdater = clusterDestinationsUpdater ?? throw new ArgumentNullException(nameof(clusterDestinationsUpdater));
        _instanceResolver = instanceResolver ?? throw new ArgumentNullException(nameof(instanceResolver));
        _configChangeListeners = configChangeListeners?.ToArray() ?? Array.Empty<IConfigChangeListener>();

        if (_providers.Length == 0)
        {
            throw new ArgumentException($"At least one {nameof(IRpcConfigProvider)} is required.", nameof(providers));
        }

        _configs = new ConfigState[_providers.Length];

        _endpointsChangeToken = new CancellationChangeToken(_endpointsChangeSource.Token);
    }

    private static IReadOnlyList<IRpcConfig> ExtractListOfProxyConfigs(IEnumerable<ConfigState> configStates)
    {
        return configStates.Select(state => state.LatestConfig).ToList().AsReadOnly();
    }

    internal async Task<IRpcStateLookup> InitialLoadAsync()
    {
        // Trigger the first load immediately and throw if it fails.
        // We intend this to crash the app so we don't try listening for further changes.
        try
        {
            var metadata = new Dictionary<string, string>();
            var services = new Dictionary<string, ServiceConfig>();

            // Begin resolving config providers concurrently.
            var resolvedConfigs = new List<(int Index, IRpcConfigProvider Provider, ValueTask<IRpcConfig> Config)>(_providers.Length);
            for (var i = 0; i < _providers.Length; i++)
            {
                var provider = _providers[i];
                var configLoadTask = LoadConfigAsync(provider, cancellationToken: default);
                resolvedConfigs.Add((i, provider, configLoadTask));
            }

            // Wait for all configs to be resolved.
            foreach (var (i, provider, configLoadTask) in resolvedConfigs)
            {
                var config = await configLoadTask;
                _configs[i] = new ConfigState(provider, config);
                metadata.PutAll(config.Metadata);
                services.PutAll(config.Services);
            }

            var proxyConfigs = ExtractListOfProxyConfigs(_configs);

            foreach (var configChangeListener in _configChangeListeners)
            {
                configChangeListener.ConfigurationLoaded(proxyConfigs);
            }

            await ApplyConfigAsync(metadata, services);

            foreach (var configChangeListener in _configChangeListeners)
            {
                configChangeListener.ConfigurationApplied(proxyConfigs);
            }

            ListenForConfigChanges();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to load or apply the proxy configuration.", ex);
        }

        //// Initial active health check is run in the background.
        //// Directly enumerate the ConcurrentDictionary to limit locking and copying.
        //_ = _activeHealthCheckMonitor.CheckHealthAsync(_clusters.Select(pair => pair.Value));
        return this;
    }

    private async Task ReloadConfigAsync()
    {
        _configChangeSource.Dispose();

        var sourcesChanged = false;
        var metadata = new Dictionary<string, string>();
        var services = new Dictionary<string, ServiceConfig>();
        var reloadedConfigs = new List<(ConfigState Config, ValueTask<IRpcConfig> ResolveTask)>();

        // Start reloading changed configurations.
        foreach (var instance in _configs)
        {
            if (instance.LatestConfig.ChangeToken.HasChanged)
            {
                try
                {
                    var reloadTask = LoadConfigAsync(instance.Provider, cancellationToken: default);
                    reloadedConfigs.Add((instance, reloadTask));
                }
                catch (Exception ex)
                {
                    OnConfigLoadError(instance, ex);
                }
            }
        }

        // Wait for all changed config providers to be reloaded.
        foreach (var (instance, loadTask) in reloadedConfigs)
        {
            try
            {
                instance.LatestConfig = await loadTask.ConfigureAwait(false);
                instance.LoadFailed = false;
                sourcesChanged = true;
            }
            catch (Exception ex)
            {
                OnConfigLoadError(instance, ex);
            }
        }

        // Extract the routes and clusters from the configs, regardless of whether they were reloaded.
        foreach (var instance in _configs)
        {
            if (instance.LatestConfig.Metadata is { Count: > 0 } updatedMetadatas)
            {
                metadata.PutAll(updatedMetadatas);
            }

            if (instance.LatestConfig.Services is { Count: > 0 } updatedServices)
            {
                services.PutAll(updatedServices);
            }
        }

        var proxyConfigs = ExtractListOfProxyConfigs(_configs);
        foreach (var configChangeListener in _configChangeListeners)
        {
            configChangeListener.ConfigurationLoaded(proxyConfigs);
        }

        try
        {
            // Only reload if at least one provider changed.
            if (sourcesChanged)
            {
                var hasChanged = await ApplyConfigAsync(metadata, services);
                //lock (_syncRoot)
                //{
                //    //// Skip if changes are signaled before the endpoints are initialized for the first time.
                //    //// The endpoint conventions might not be ready yet.
                //    //if (hasChanged && _endpoints is not null)
                //    //{
                //    //    CreateEndpoints();
                //    //}
                //}
            }

            foreach (var configChangeListener in _configChangeListeners)
            {
                configChangeListener.ConfigurationApplied(proxyConfigs);
            }
        }
        catch (Exception ex)
        {
            Log.ErrorApplyingConfig(_logger, ex);

            foreach (var configChangeListener in _configChangeListeners)
            {
                configChangeListener.ConfigurationApplyingFailed(proxyConfigs, ex);
            }
        }

        ListenForConfigChanges();

        void OnConfigLoadError(ConfigState instance, Exception ex)
        {
            instance.LoadFailed = true;
            Log.ErrorReloadingConfig(_logger, ex);

            foreach (var configChangeListener in _configChangeListeners)
            {
                configChangeListener.ConfigurationLoadingFailed(instance.Provider, ex);
            }
        }
    }

    private static void ValidateConfigProperties(IRpcConfig config)
    {
        if (config is null)
        {
            throw new InvalidOperationException($"{nameof(IRpcConfigProvider.GetConfig)} returned a null value.");
        }

        if (config.ChangeToken is null)
        {
            throw new InvalidOperationException($"{nameof(IRpcConfig.ChangeToken)} has a null value.");
        }
    }

    private ValueTask<IRpcConfig> LoadConfigAsync(IRpcConfigProvider provider, CancellationToken cancellationToken)
    {
        var config = provider.GetConfig();
        ValidateConfigProperties(config);

        if (_instanceResolver.GetType() == typeof(NoOpInstanceResolver))
        {
            return new(config);
        }

        return LoadConfigAsyncCore(config, cancellationToken);
    }

    private async ValueTask<IRpcConfig> LoadConfigAsyncCore(IRpcConfig config, CancellationToken cancellationToken)
    {
        List<(int Index, ValueTask<ResolvedInstanceCollection> Task)> resolverTasks = new();
        Dictionary<string, ServiceConfig> services = new(config.Services);
        List<IChangeToken>? changeTokens = null;
        for (var i = 0; i < services.Count; i++)
        {
            var service = services.ElementAt(i);
            if (service.Value.Instances is { Count: > 0 } instances)
            {
                // Resolve destinations if there are any.
                var task = _instanceResolver.ResolveInstancesAsync(instances, cancellationToken);
                resolverTasks.Add((i, task));
            }
        }

        if (resolverTasks.Count > 0)
        {
            foreach (var (i, task) in resolverTasks)
            {
                ResolvedInstanceCollection resolvedInstances;
                var service = services.ElementAt(i);
                try
                {
                    resolvedInstances = await task;
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Error resolving destinations for cluster {service.Key}", exception);
                }

                services[service.Key] = services[service.Key] with { Instances = resolvedInstances.Instances };
                if (resolvedInstances.ChangeToken is { } token)
                {
                    changeTokens ??= new();
                    changeTokens.Add(token);
                }
            }

            IChangeToken changeToken;
            if (changeTokens is not null)
            {
                // Combine change tokens from the resolver with the configuration's existing change token.
                changeTokens.Add(config.ChangeToken);
                changeToken = new CompositeChangeToken(changeTokens);
            }
            else
            {
                changeToken = config.ChangeToken;
            }

            // Return updated config
            return new ResolvedProxyConfig(config, services, changeToken);
        }

        return config;
    }

    private sealed class ResolvedProxyConfig(IRpcConfig _innerConfig, IReadOnlyDictionary<string, ServiceConfig> services, IChangeToken changeToken) : IRpcConfig
    {
        public IReadOnlyList<string> Contracts => _innerConfig.Contracts;
        public IReadOnlyDictionary<string, string> Metadata => _innerConfig.Metadata;

        public IReadOnlyDictionary<string, ServiceConfig> Services { get; } = services;

        public IChangeToken ChangeToken { get; } = changeToken;

    }

    private void ListenForConfigChanges()
    {
        // Use a central change token to avoid overlap between different sources.
        var source = new CancellationTokenSource();
        _configChangeSource = source;
        var poll = false;

        foreach (var configState in _configs)
        {
            if (configState.LoadFailed)
            {
                // We can't register for change notifications if the last load failed.
                poll = true;
                continue;
            }

            configState.CallbackCleanup?.Dispose();
            var token = configState.LatestConfig.ChangeToken;
            if (token.ActiveChangeCallbacks)
            {
                configState.CallbackCleanup = token.RegisterChangeCallback(SignalChange, source);
            }
            else
            {
                poll = true;
            }
        }

        if (poll)
        {
            source.CancelAfter(TimeSpan.FromMinutes(5));
        }

        // Don't register until we're done hooking everything up to avoid cancellation races.
        source.Token.Register(ReloadConfig, this);

        static void SignalChange(object? obj)
        {
            var token = (CancellationTokenSource)obj!;
            try
            {
                token.Cancel();
            }
            // Don't throw if the source was already disposed.
            catch (ObjectDisposedException) { }
        }

        static void ReloadConfig(object? state)
        {
            var manager = (RpcConfigManager)state!;
            _ = manager.ReloadConfigAsync();
            manager.InvokeChanged();
        }
    }

    // Throws for validation failures
    private async Task<bool> ApplyConfigAsync(IReadOnlyDictionary<string, string> metadata, IReadOnlyDictionary<string, ServiceConfig> services)
    {
        var (configuredServices, serviceErrors) = await VerifyServicesAsync(services, cancellation: default);
        //var (configuredRoutes, routeErrors) = await VerifyMetadataAsync(metadata, configuredClusters, cancellation: default);

        //if (routeErrors.Count > 0 || clusterErrors.Count > 0)
        //{
        //    throw new AggregateException("The proxy config is invalid.", routeErrors.Concat(clusterErrors));
        //}
        if (serviceErrors.Count > 0)
        {
            throw new AggregateException("The proxy config is invalid.", serviceErrors);
        }
        // Update clusters first because routes need to reference them.
        UpdateRuntimeServices(configuredServices);
        //var routesChanged = UpdateRuntimeMetadata(metadata);
        //return routesChanged;
        return true;
    }


    private async Task<(IReadOnlyDictionary<string, ServiceConfig>, IList<Exception>)> VerifyServicesAsync(IReadOnlyDictionary<string, ServiceConfig> services, CancellationToken cancellation)
    {
        if (services is null)
        {
            return (_emptyServiceDictionary, Array.Empty<Exception>());
        }

        var configuredServices = new Dictionary<string, ServiceConfig>(services.Count, StringComparer.OrdinalIgnoreCase);
        var errors = new List<Exception>();
        // The IProxyConfigProvider provides a fresh snapshot that we need to reconfigure each time.
        foreach (var c in services)
        {
            try
            {
                if (configuredServices.ContainsKey(c.Key))
                {
                    errors.Add(new ArgumentException($"Duplicate service '{c.Key}'."));
                    continue;
                }

                // Don't modify the original
                var service = c.Value;

                foreach (var filter in _filters)
                {
                    service = await filter.ConfigureServiceAsync(service, cancellation);
                }

                var serviceErrors = await _configValidator.ValidateServiceAsync(service);
                if (serviceErrors.Count > 0)
                {
                    errors.AddRange(serviceErrors);
                    continue;
                }
                configuredServices.Add(c.Key, service);
            }
            catch (Exception ex)
            {
                errors.Add(new ArgumentException($"An exception was thrown from the configuration callbacks for cluster '{c.Key}'.", ex));
            }
        }

        if (errors.Count > 0)
        {
            return (_emptyServiceDictionary, errors);
        }

        return (configuredServices, errors);
    }
    private void UpdateRuntimeServices(IReadOnlyDictionary<string, ServiceConfig> incomingServices)
    {
        var desiredServices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var incomingService in incomingServices)
        {
            var added = desiredServices.Add(incomingService.Key);
            Debug.Assert(added);

            if (_services.TryGetValue(incomingService.Key, out var currentService))
            {
                var instancesChanged = UpdateRuntimeInstances(incomingService.Value.Instances, currentService.Instances);

                var currentServiceModel = currentService.Model;

                //var httpClient = _httpClientFactory.CreateClient(new ForwarderHttpClientContext
                //{
                //    ClusterId = currentCluster.ClusterId,
                //    OldConfig = currentClusterModel.Config.HttpClient ?? HttpClientConfig.Empty,
                //    OldMetadata = currentClusterModel.Config.Metadata,
                //    OldClient = currentClusterModel.HttpClient,
                //    NewConfig = incomingCluster.HttpClient ?? HttpClientConfig.Empty,
                //    NewMetadata = incomingCluster.Metadata
                //});

                var newServiceModel = new ServiceModel(incomingService.Value, null);

                // Excludes destination changes, they're tracked separately.
                var configChanged = currentServiceModel.HasConfigChanged(newServiceModel);
                if (configChanged)
                {
                    currentService.Revision++;
                    Log.ServiceChanged(_logger, incomingService.Key);
                }

                if (instancesChanged || configChanged)
                {
                    // Config changed, so update runtime cluster
                    currentService.Model = newServiceModel;

                    //_clusterDestinationsUpdater.UpdateAllDestinations(currentCluster);

                    //foreach (var listener in _clusterChangeListeners)
                    //{
                    //    listener.OnClusterChanged(currentCluster);
                    //}
                }
            }
            else
            {
                var newServiceState = new ServiceState(incomingService.Key);

                UpdateRuntimeInstances(incomingService.Value.Instances, newServiceState.Instances);

                //var httpClient = _httpClientFactory.CreateClient(new ForwarderHttpClientContext
                //{
                //    ClusterId = newClusterState.ClusterId,
                //    NewConfig = incomingCluster.HttpClient ?? HttpClientConfig.Empty,
                //    NewMetadata = incomingCluster.Metadata
                //});

                newServiceState.Model = new ServiceModel(incomingService.Value, null);
                newServiceState.Revision++;
                Log.ServiceAdded(_logger, incomingService.Key);

                //_clusterDestinationsUpdater.UpdateAllDestinations(newClusterState);

                added = _services.TryAdd(incomingService.Key, newServiceState);
                Debug.Assert(added);

                //foreach (var listener in _clusterChangeListeners)
                //{
                //    listener.OnClusterAdded(newClusterState);
                //}
            }
        }

        // Directly enumerate the ConcurrentDictionary to limit locking and copying.
        foreach (var existingServicePairKey in _services.Keys)
        {
            var existingService = _services[existingServicePairKey];
            if (!desiredServices.Contains(existingService.ServiceName))
            {
                // NOTE 1: Remove is safe to do within the `foreach` loop on ConcurrentDictionary
                //
                // NOTE 2: Removing the cluster from _clusters is safe and existing
                // ASP .NET Core endpoints will continue to work with their existing behavior (until those endpoints are updated)
                // and the Garbage Collector won't destroy this cluster object while it's referenced elsewhere.
                Log.ServiceRemoved(_logger, existingService.ServiceName);
                var removed = _services.TryRemove(existingService.ServiceName, out var _);
                Debug.Assert(removed);

                //foreach (var listener in _serviceChangeListeners)
                //{
                //    listener.OnServiceRemoved(existingService);
                //}
            }
        }
    }

    private bool UpdateRuntimeInstances(IReadOnlyDictionary<string, InstanceConfig>? incomingInstances, ConcurrentDictionary<string, InstanceState> currentInstances)
    {
        var desiredInstances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changed = false;

        if (incomingInstances is not null)
        {
            foreach (var incomingInstance in incomingInstances)
            {
                var added = desiredInstances.Add(incomingInstance.Key);
                Debug.Assert(added);

                if (currentInstances.TryGetValue(incomingInstance.Key, out var currentInstance))
                {
                    if (currentInstance.Model.HasChanged(incomingInstance.Value))
                    {
                        Log.InstanceChanged(_logger, incomingInstance.Key);
                        currentInstance.Model = new InstanceModel(incomingInstance.Value);
                        changed = true;
                    }
                }
                else
                {
                    Log.InstanceAdded(_logger, incomingInstance.Key);
                    var newDestination = new InstanceState(incomingInstance.Key)
                    {
                        Model = new InstanceModel(incomingInstance.Value),
                    };
                    added = currentInstances.TryAdd(incomingInstance.Key, newDestination);
                    Debug.Assert(added);
                    changed = true;
                }
            }
        }

        // Directly enumerate the ConcurrentDictionary to limit locking and copying.
        foreach (var existingDestinationPair in currentInstances)
        {
            var id = existingDestinationPair.Value.InstanceId;
            if (!desiredInstances.Contains(id))
            {
                // NOTE 1: Remove is safe to do within the `foreach` loop on ConcurrentDictionary
                //
                // NOTE 2: Removing the endpoint from `IEndpointManager` is safe and existing
                // clusters will continue to work with their existing behavior (until those clusters are updated)
                // and the Garbage Collector won't destroy this cluster object while it's referenced elsewhere.
                Log.DestinationRemoved(_logger, id);
                var removed = currentInstances.TryRemove(id, out var _);
                Debug.Assert(removed);
                changed = true;
            }
        }

        return changed;
    }

    public void Dispose()
    {
        _configChangeSource.Dispose();
        foreach (var instance in _configs)
        {
            instance?.CallbackCleanup?.Dispose();
        }
    }

    public IReadOnlyCollection<string> GetContracts() => _contracts;

    public IReadOnlyDictionary<string, string> GetMetadata() => _metadata;

    public IReadOnlyDictionary<string, ServiceState> GetServices()
    {
        return _services;
    }

    internal event Action<IRpcStateLookup>? _onChange;
    public IDisposable? OnChange(Action<IRpcStateLookup> listener)
    {
        var changeTrackerDisposable = new ChangeTrackerDisposable(this, listener);
        _onChange += changeTrackerDisposable.OnChange;
        return changeTrackerDisposable;
    }

    private void InvokeChanged()
    {
        this._onChange?.Invoke(this);
    }

    internal sealed class ChangeTrackerDisposable : IDisposable
    {
        private readonly Action<IRpcStateLookup> _listener;

        private readonly RpcConfigManager _monitor;

        public ChangeTrackerDisposable(RpcConfigManager monitor, Action<IRpcStateLookup> listener)
        {
            _listener = listener;
            _monitor = monitor;
        }

        public void OnChange(IRpcStateLookup options)
        {
            _listener(options);
        }

        public void Dispose()
        {
            _monitor._onChange -= OnChange;
        }
    }

    private class ConfigState(IRpcConfigProvider provider, IRpcConfig config)
    {
        public IRpcConfigProvider Provider { get; } = provider;

    public IRpcConfig LatestConfig { get; set; } = config;

    public bool LoadFailed { get; set; }

        public IDisposable? CallbackCleanup { get; set; }
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception?> _clusterAdded = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.ClusterAdded,
            "Cluster '{clusterId}' has been added.");

        private static readonly Action<ILogger, string, Exception?> _clusterChanged = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.ClusterChanged,
            "Cluster '{clusterId}' has changed.");

        private static readonly Action<ILogger, string, Exception?> _clusterRemoved = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.ClusterRemoved,
            "Cluster '{clusterId}' has been removed.");

        private static readonly Action<ILogger, string, Exception?> _serviceAdded = LoggerMessage.Define<string>(
    LogLevel.Debug,
    EventIds.ClusterAdded,
    "Service '{serviceName}' has been added.");

        private static readonly Action<ILogger, string, Exception?> _serviceChanged = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.ClusterChanged,
            "Service '{serviceName}' has changed.");

        private static readonly Action<ILogger, string, Exception?> _serviceRemoved = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.ClusterRemoved,
            "Service '{serviceName}' has been removed.");

        private static readonly Action<ILogger, string, Exception?> _instanceAdded = LoggerMessage.Define<string>(
    LogLevel.Debug,
    EventIds.DestinationAdded,
    "Instance '{destinationId}' has been added.");

        private static readonly Action<ILogger, string, Exception?> _instanceChanged = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.DestinationChanged,
            "Instance '{destinationId}' has changed.");

        private static readonly Action<ILogger, string, Exception?> _instanceRemoved = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.DestinationRemoved,
            "Instance '{destinationId}' has been removed.");

        private static readonly Action<ILogger, string, Exception?> _destinationAdded = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.DestinationAdded,
            "Destination '{destinationId}' has been added.");

        private static readonly Action<ILogger, string, Exception?> _destinationChanged = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.DestinationChanged,
            "Destination '{destinationId}' has changed.");

        private static readonly Action<ILogger, string, Exception?> _destinationRemoved = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.DestinationRemoved,
            "Destination '{destinationId}' has been removed.");

        private static readonly Action<ILogger, string, Exception?> _routeAdded = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.RouteAdded,
            "Route '{routeId}' has been added.");

        private static readonly Action<ILogger, string, Exception?> _routeChanged = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.RouteChanged,
            "Route '{routeId}' has changed.");

        private static readonly Action<ILogger, string, Exception?> _routeRemoved = LoggerMessage.Define<string>(
            LogLevel.Debug,
            EventIds.RouteRemoved,
            "Route '{routeId}' has been removed.");

        private static readonly Action<ILogger, Exception> _errorReloadingConfig = LoggerMessage.Define(
            LogLevel.Error,
            EventIds.ErrorReloadingConfig,
            "Failed to reload config. Unable to register for change notifications, polling for changes until successful.");

        private static readonly Action<ILogger, Exception> _errorApplyingConfig = LoggerMessage.Define(
            LogLevel.Error,
            EventIds.ErrorApplyingConfig,
            "Failed to apply the new config.");

        public static void ClusterAdded(ILogger logger, string clusterId)
        {
            _clusterAdded(logger, clusterId, null);
        }

        public static void ClusterChanged(ILogger logger, string clusterId)
        {
            _clusterChanged(logger, clusterId, null);
        }

        public static void ClusterRemoved(ILogger logger, string clusterId)
        {
            _clusterRemoved(logger, clusterId, null);
        }

        public static void ServiceAdded(ILogger logger, string clusterId)
        {
            _serviceAdded(logger, clusterId, null);
        }

        public static void ServiceChanged(ILogger logger, string clusterId)
        {
            _serviceChanged(logger, clusterId, null);
        }

        public static void ServiceRemoved(ILogger logger, string clusterId)
        {
            _serviceRemoved(logger, clusterId, null);
        }

        public static void InstanceAdded(ILogger logger, string destinationId)
        {
            _instanceAdded(logger, destinationId, null);
        }

        public static void InstanceChanged(ILogger logger, string destinationId)
        {
            _instanceChanged(logger, destinationId, null);
        }

        public static void InstanceRemoved(ILogger logger, string destinationId)
        {
            _instanceRemoved(logger, destinationId, null);
        }

        public static void DestinationAdded(ILogger logger, string destinationId)
        {
            _destinationAdded(logger, destinationId, null);
        }

        public static void DestinationChanged(ILogger logger, string destinationId)
        {
            _destinationChanged(logger, destinationId, null);
        }

        public static void DestinationRemoved(ILogger logger, string destinationId)
        {
            _destinationRemoved(logger, destinationId, null);
        }

        public static void RouteAdded(ILogger logger, string routeId)
        {
            _routeAdded(logger, routeId, null);
        }

        public static void RouteChanged(ILogger logger, string routeId)
        {
            _routeChanged(logger, routeId, null);
        }

        public static void RouteRemoved(ILogger logger, string routeId)
        {
            _routeRemoved(logger, routeId, null);
        }

        public static void ErrorReloadingConfig(ILogger logger, Exception ex)
        {
            _errorReloadingConfig(logger, ex);
        }

        public static void ErrorApplyingConfig(ILogger logger, Exception ex)
        {
            _errorApplyingConfig(logger, ex);
        }
    }
}
