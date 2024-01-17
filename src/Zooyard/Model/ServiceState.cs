using System.Collections.Concurrent;
using Zooyard.Atomic;

namespace Zooyard.Model;

/// <summary>
/// Representation of a cluster for use at runtime.
/// </summary>
public sealed class ServiceState
{
    private volatile ServiceInstancesState _instancesState = new (Array.Empty<InstanceState>(), Array.Empty<InstanceState>());
    private volatile ServiceModel _model = default!; // Initialized right after construction.

    /// <summary>
    /// Creates a new instance. This constructor is for tests and infrastructure, this type is normally constructed by the configuration
    /// loading infrastructure.
    /// </summary>
    public ServiceState(string serviceName)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
    }

    /// <summary>
    /// Constructor overload to additionally initialize the <see cref="ServiceModel"/> for tests and infrastructure
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public ServiceState(string serviceName, ServiceModel model) : this(serviceName)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// The cluster's unique id.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Encapsulates parts of a cluster that can change atomically in reaction to config changes.
    /// </summary>
    public ServiceModel Model
    {
        get => _model;
        internal set => _model = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// All of the destinations associated with this cluster. This collection is populated by the configuration system
    /// and should only be directly modified in a test environment.
    /// Call IClusterDestinationsUpdater after modifying this collection.
    /// </summary>
    public ConcurrentDictionary<string, InstanceState> Instances { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Stores the state of cluster's destinations that can change atomically
    /// in reaction to runtime state changes (e.g. changes of destinations' health).
    /// </summary>
    public ServiceInstancesState InstancesState
    {
        get => _instancesState;
        set => _instancesState = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Keeps track of the total number of concurrent requests on this cluster.
    /// </summary>
    internal AtomicCounter ConcurrencyCounter { get; } = new AtomicCounter();

    /// <summary>
    /// Tracks changes to the cluster configuration for use with rebuilding dependent endpoints. Destination changes do not affect this property.
    /// </summary>
    internal int Revision { get; set; }
}
