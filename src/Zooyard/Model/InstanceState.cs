using System.Collections;
using Zooyard.Atomic;

namespace Zooyard.Model;

public sealed class InstanceState : IReadOnlyList<InstanceState>
{
    private volatile InstanceModel _model = default!;

    /// <summary>
    /// Creates a new instance. This constructor is for tests and infrastructure, this type is normally constructed by
    /// the configuration loading infrastructure.
    /// </summary>
    public InstanceState(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            throw new ArgumentNullException(nameof(instanceId));
        }
        InstanceId = instanceId;
    }

    /// <summary>
    /// Constructor overload to additionally initialize the <see cref="InstanceModel"/> for tests and infrastructure
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public InstanceState(string instanceId, InstanceModel model) : this(instanceId)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// The destination's unique id.
    /// </summary>
    public string InstanceId { get; }

    /// <summary>
    /// A snapshot of the current configuration
    /// </summary>
    public InstanceModel Model
    {
        get => _model;
        internal set => _model = value ?? throw new ArgumentNullException(nameof(value));
    }

    ///// <summary>
    ///// Mutable health state for this destination.
    ///// </summary>
    //public DestinationHealthState Health { get; } = new DestinationHealthState();

    /// <summary>
    /// Keeps track of the total number of concurrent requests on this endpoint.
    /// The setter should only be used for testing purposes.
    /// </summary>
    public int ConcurrentRequestCount
    {
        get => ConcurrencyCounter.Value;
        set => ConcurrencyCounter.Value = value;
    }

    internal AtomicCounter ConcurrencyCounter { get; } = new AtomicCounter();

    InstanceState IReadOnlyList<InstanceState>.this[int index]
        => index == 0 ? this : throw new IndexOutOfRangeException();

    int IReadOnlyCollection<InstanceState>.Count => 1;

    private Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<InstanceState> IEnumerable<InstanceState>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private struct Enumerator : IEnumerator<InstanceState>
    {
        private bool _read;

        internal Enumerator(InstanceState instance)
        {
            Current = instance;
            _read = false;
        }

        public InstanceState Current { get; }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (!_read)
            {
                _read = true;
                return true;
            }
            return false;
        }

        public void Dispose()
        {

        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }
}
