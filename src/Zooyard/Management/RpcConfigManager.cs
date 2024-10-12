using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Zooyard.Configuration;
using Zooyard.Model;
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

    //private readonly IClusterChangeListener[] _clusterChangeListeners;
    private readonly ConcurrentDictionary<string, ServiceState> _services = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RouteState> _routes = new(StringComparer.OrdinalIgnoreCase);

    private readonly IRpcConfigFilter[] _filters;
    private readonly IConfigValidator _configValidator;
    //private readonly IServiceInstancesUpdater _clusterDestinationsUpdater;
    private readonly IInstanceResolver _instanceResolver;
    private readonly IConfigChangeListener[] _configChangeListeners;
    private IChangeToken _endpointsChangeToken;

    private CancellationTokenSource _configChangeSource = new();

    public RpcConfigManager(
        ILogger<RpcConfigManager> logger
        , IEnumerable<IRpcConfigProvider> providers
        , IEnumerable<IRpcConfigFilter> filters
        , IConfigValidator configValidator
        , IEnumerable<IConfigChangeListener> configChangeListeners //IServiceInstancesUpdater clusterDestinationsUpdater,
        , IInstanceResolver instanceResolver
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = providers?.ToArray() ?? throw new ArgumentNullException(nameof(providers));
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

        AsyncHelper.RunSync(InitialLoadAsync);
        // Register these last as the callbacks could run immediately
        //appLifetime.ApplicationStarted.Register(Start);
        //appLifetime.ApplicationStopping.Register(Close);
    }

    //public void Start()
    //{
    //    _ = InitialLoadAsync();
    //}

    //public void Close()
    //{
    //    Dispose();
    //}

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
            var routes = new List<RouteConfig>();
            var services = new List<ServiceConfig>();

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

                routes.AddRangeCombined(config.Routes);
                services.AddRangeCombined(config.Services);
            }

            var proxyConfigs = ExtractListOfProxyConfigs(_configs);

            foreach (var configChangeListener in _configChangeListeners)
            {
                configChangeListener.ConfigurationLoaded(proxyConfigs);
            }

            await ApplyConfigAsync(routes, services);

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
        var routes = new List<RouteConfig>();
        var services = new List<ServiceConfig>();
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
            if (instance.LatestConfig.Routes is { Count: > 0 } updatedMetadatas)
            {
                routes.AddRangeCombined(updatedMetadatas);
            }

            if (instance.LatestConfig.Services is { Count: > 0 } updatedServices)
            {
                services.AddRangeCombined(updatedServices);
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
                var hasChanged = await ApplyConfigAsync(routes, services);
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
        List<ServiceConfig> services = new(config.Services);
        List<IChangeToken>? changeTokens = null;
        for (var i = 0; i < services.Count; i++)
        {
            var service = services[i];
            if (service.Instances is { Count: > 0 } instances)
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
                try
                {
                    resolvedInstances = await task;
                }
                catch (Exception exception)
                {
                    var service = services[i];
                    throw new InvalidOperationException($"Error resolving destinations for service {service.ServiceId}", exception);
                }

                services[i] = services[i] with { Instances = resolvedInstances.Instances };
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

    private sealed class ResolvedProxyConfig(IRpcConfig _innerConfig
        , IReadOnlyList<ServiceConfig> services
        , IChangeToken changeToken)
        : IRpcConfig
    {
        public IReadOnlyList<RouteConfig> Routes => _innerConfig.Routes;

        public IReadOnlyList<ServiceConfig> Services { get; } = services;

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
    private async Task<bool> ApplyConfigAsync(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ServiceConfig> services)
    {
        var (configuredServices, serviceErrors) = await VerifyServicesAsync(services, cancellation: default);
        var (configuredRoutes, publicErrors) = await VerifyRoutesAsync(routes, cancellation: default);

        if (publicErrors.Count > 0 || serviceErrors.Count > 0)
        {
            throw new AggregateException("The rpc config is invalid.", publicErrors.Concat(serviceErrors));
        }
        // Update clusters first because routes need to reference them.
        UpdateRuntimeServices(configuredServices.Values);
        var routesChanged = UpdateRuntimeRoutes(configuredRoutes);
        return routesChanged;
    }

    private async Task<(IList<RouteConfig>, IList<Exception>)> VerifyRoutesAsync(IReadOnlyList<RouteConfig> routes, CancellationToken cancellation)
    {
        if (routes is null)
        {
            return (Array.Empty<RouteConfig>(), Array.Empty<Exception>());
        }

        var seenRouteIds = new HashSet<string>(routes.Count, StringComparer.OrdinalIgnoreCase);
        var configuredRoutes = new List<RouteConfig>(routes.Count);
        var errors = new List<Exception>();

        foreach (var r in routes)
        {
            if (seenRouteIds.Contains(r.RouteId))
            {
                errors.Add(new ArgumentException($"Duplicate route '{r.RouteId}'"));
                continue;
            }

            var pub = r;

            try
            {
                if (_filters.Length != 0)
                {
                    foreach (var filter in _filters)
                    {
                        pub = await filter.ConfigureRouteAsync(pub, cancellation);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new Exception($"An exception was thrown from the configuration callbacks for route '{r.RouteId}'.", ex));
                continue;
            }

            var publicErrors = await _configValidator.ValidateRouteAsync(pub);
            if (publicErrors.Count > 0)
            {
                errors.AddRange(publicErrors);
                continue;
            }

            seenRouteIds.Add(pub.RouteId);
            configuredRoutes.Add(pub);
        }

        if (errors.Count > 0)
        {
            return (Array.Empty<RouteConfig>(), errors);
        }

        return (configuredRoutes, errors);
    }


    private async Task<(IReadOnlyDictionary<string, ServiceConfig>, IList<Exception>)> VerifyServicesAsync(IReadOnlyList<ServiceConfig> services, CancellationToken cancellation)
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
                if (configuredServices.ContainsKey(c.ServiceId))
                {
                    errors.Add(new ArgumentException($"Duplicate service '{c.ServiceId}'."));
                    continue;
                }

                // Don't modify the original
                var service = c;

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
                configuredServices.Add(c.ServiceId, service);
            }
            catch (Exception ex)
            {
                errors.Add(new ArgumentException($"An exception was thrown from the configuration callbacks for service '{c.ServiceId}'.", ex));
            }
        }

        if (errors.Count > 0)
        {
            return (_emptyServiceDictionary, errors);
        }

        return (configuredServices, errors);
    }
    private void UpdateRuntimeServices(IEnumerable<ServiceConfig> incomingServices)
    {
        var desiredServices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var incomingService in incomingServices)
        {
            var added = desiredServices.Add(incomingService.ServiceId);
            Debug.Assert(added);

            if (_services.TryGetValue(incomingService.ServiceId, out var currentService))
            {
                var instancesChanged = UpdateRuntimeInstances(incomingService.Instances, currentService.Instances);

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

                var newServiceModel = new ServiceModel(incomingService);//, null);

                // Excludes destination changes, they're tracked separately.
                var configChanged = currentServiceModel.HasConfigChanged(newServiceModel);
                if (configChanged)
                {
                    currentService.Revision++;
                    Log.ServiceChanged(_logger, incomingService.ServiceId);
                }

                if (instancesChanged || configChanged)
                {
                    // Config changed, so update runtime cluster
                    currentService.Model = newServiceModel;

                    //_clusterDestinationsUpdater.UpdateAllDestinations(currentService);

                    //foreach (var listener in _serviceChangeListeners)
                    //{
                    //    listener.OnClusterChanged(currentService);
                    //}
                }
            }
            else
            {
                var newServiceState = new ServiceState(incomingService.ServiceId);

                UpdateRuntimeInstances(incomingService.Instances, newServiceState.Instances);

                //var httpClient = _httpClientFactory.CreateClient(new ForwarderHttpClientContext
                //{
                //    ClusterId = newClusterState.ClusterId,
                //    NewConfig = incomingCluster.HttpClient ?? HttpClientConfig.Empty,
                //    NewMetadata = incomingCluster.Metadata
                //});

                newServiceState.Model = new ServiceModel(incomingService);//, null);
                newServiceState.Revision++;
                Log.ServiceAdded(_logger, incomingService.ServiceId);

                //_clusterDestinationsUpdater.UpdateAllDestinations(newClusterState);

                added = _services.TryAdd(incomingService.ServiceId, newServiceState);
                Debug.Assert(added);

                //foreach (var listener in _clusterChangeListeners)
                //{
                //    listener.OnClusterAdded(newClusterState);
                //}
            }
        }

        // Directly enumerate the ConcurrentDictionary to limit locking and copying.
        foreach (var existingClusterPair in _services)
        {
            var existingService = existingClusterPair.Value;
            if (!desiredServices.Contains(existingService.ServiceId))
            {
                // NOTE 1: Remove is safe to do within the `foreach` loop on ConcurrentDictionary
                //
                // NOTE 2: Removing the cluster from _clusters is safe and existing
                // ASP .NET Core endpoints will continue to work with their existing behavior (until those endpoints are updated)
                // and the Garbage Collector won't destroy this cluster object while it's referenced elsewhere.
                Log.ServiceRemoved(_logger, existingService.ServiceId);
                var removed = _services.TryRemove(existingService.ServiceId, out var _);
                Debug.Assert(removed);

                //foreach (var listener in _serviceChangeListeners)
                //{
                //    listener.OnServiceRemoved(existingService);
                //}
            }
        }
    }

    private bool UpdateRuntimeInstances(IDictionary<string, InstanceConfig>? incomingInstances, ConcurrentDictionary<string, InstanceState> currentInstances)
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

    private bool UpdateRuntimeRoutes(IList<RouteConfig> incomingRoutes)
    {
        var desiredRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changed = false;

        foreach (var incomingRoute in incomingRoutes)
        {
            desiredRoutes.Add(incomingRoute.RouteId);

            //// Note that this can be null, and that is fine. The resulting route may match
            //// but would then fail to route, which is exactly what we were instructed to do in this case
            //// since no valid cluster was specified.
            //_services.TryGetValue(incomingRoute.ClusterId ?? string.Empty, out var cluster);

            if (_routes.TryGetValue(incomingRoute.RouteId, out var currentRoute))
            {
                if (currentRoute.Model.HasConfigChanged(incomingRoute))
                {
                    //currentRoute.CachedEndpoint = null; // Recreate endpoint
                    var newModel = BuildRouteModel(incomingRoute);//, cluster);
                    currentRoute.Model = newModel;
                    //currentRoute.ClusterRevision = cluster?.Revision;
                    changed = true;
                    Log.RouteChanged(_logger, currentRoute.RouteId);
                }
            }
            else
            {
                var newModel = BuildRouteModel(incomingRoute);//, cluster);
                var newState = new RouteState(incomingRoute.RouteId)
                {
                    Model = newModel,
                    //ClusterRevision = cluster?.Revision,
                };
                var added = _routes.TryAdd(newState.RouteId, newState);
                Debug.Assert(added);
                changed = true;
                Log.RouteAdded(_logger, newState.RouteId);
            }
        }

        // Directly enumerate the ConcurrentDictionary to limit locking and copying.
        foreach (var existingRoutePair in _routes)
        {
            var routeId = existingRoutePair.Value.RouteId;
            if (!desiredRoutes.Contains(routeId))
            {
                // NOTE 1: Remove is safe to do within the `foreach` loop on ConcurrentDictionary
                //
                // NOTE 2: Removing the route from _routes is safe and existing
                // ASP.NET Core endpoints will continue to work with their existing behavior since
                // their copy of `RouteModel` is immutable and remains operational in whichever state is was in.
                Log.RouteRemoved(_logger, routeId);
                var removed = _routes.TryRemove(routeId, out var _);
                Debug.Assert(removed);
                changed = true;
            }
        }

        return changed;
    }

    private RouteModel BuildRouteModel(RouteConfig source)//, ServiceState? cluster)
    {
        //var transforms = _transformBuilder.Build(source, cluster?.Model?.Config);

        return new RouteModel(source);//, cluster, transforms);
    }

    public bool TryGetRoute(string id, [NotNullWhen(true)] out RouteModel? route)
    {
        if (_routes.TryGetValue(id, out var routeState))
        {
            route = routeState.Model;
            return true;
        }

        route = null;
        return false;
    }

    public IEnumerable<RouteModel> GetRoutes()
    {
        foreach (var (_, route) in _routes)
        {
            yield return route.Model;
        }
    }

    public bool TryGetService(string id, [NotNullWhen(true)] out ServiceState? service)
    {
        return _services.TryGetValue(id, out service!);
    }

    public IEnumerable<ServiceState> GetServices()
    {
        foreach (var (_, service) in _services)
        {
            yield return service;
        }
    }

    public void Dispose()
    {
        _configChangeSource.Dispose();
        foreach (var instance in _configs)
        {
            instance?.CallbackCleanup?.Dispose();
        }
    }

    internal event Action<IRpcStateLookup>? _onChange;
    /// <summary>
    /// 注册修改事件
    /// </summary>
    /// <param name="listener"></param>
    /// <returns></returns>
    public IDisposable OnChange(Action<IRpcStateLookup> listener)
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



public static class RpcConfigManagerExtend
{
    public static void AddRangeCombined(this List<RouteConfig> _this, IEnumerable<RouteConfig>? other) 
    {
        if (other == null) { return; }
        foreach (var item in other) 
        {
            var config = _this.FirstOrDefault(w => w.RouteId == item.RouteId);
            if (config == null) 
            {
                _this.Add(item);
                continue;
            }

            config.ServicePattern = item.ServicePattern;

            foreach (var meta in item.Metadata)
            {
                config.Metadata[meta.Key] = meta.Value;
            }
        }
    }
    public static void AddRangeCombined(this List<ServiceConfig> _this, IEnumerable<ServiceConfig>? other)
    {
        if (other == null) { return; }
        foreach (var item in other)
        {
            var config = _this.FirstOrDefault(w => w.ServiceId == item.ServiceId);
            if (config == null)
            {
                _this.Add(item);
                continue;
            }


            foreach (var instance in item.Instances)
            {
                if (!config.Instances.ContainsKey(instance.Key))
                {
                    config.Instances.Add(instance.Key, instance.Value);
                }

                config.Instances[instance.Key].Host = instance.Value.Host;
                config.Instances[instance.Key].Port = instance.Value.Port;

                foreach (var meta in instance.Value.Metadata)
                {
                    config.Instances[instance.Key].Metadata[meta.Key] = meta.Value;
                }
            }

            foreach (var meta in item.Metadata)
            {
                config.Metadata[meta.Key] = meta.Value;
            }
        }
    }
}
