namespace Zooyard.Atomic;

internal sealed class AtomicReference<T> where T : class
{
    T? _atomicValue;

    /// <summary>
    /// Sets the initial value of this <see cref="AtomicReference{T}" /> to .
    /// </summary>
    public AtomicReference(T originalValue)
    {
        _atomicValue = originalValue;
    }

    /// <summary>
    ///     Default constructor
    /// </summary>
    public AtomicReference()
    {
        _atomicValue = default;
    }

    /// <summary>
    ///     The current value of this <see cref="AtomicReference{T}" />
    /// </summary>
    public T? Value
    {
        get { return Volatile.Read(ref _atomicValue); }
        set { Volatile.Write(ref _atomicValue, value); }
    }

    /// <summary>
    ///     If Value equals expected, then set the Value to
    ///     newValue.
    ///     Returns true if newValue was set, false otherwise.
    /// </summary>
    public bool CompareAndSet(T expected, T newValue) => Interlocked.CompareExchange(ref _atomicValue, newValue, expected) == expected;

    #region Conversion operators

    /// <summary>
    ///     Implicit conversion operator = automatically casts the <see cref="AtomicReference{T}" /> to an instance of
    /// </summary>
    public static implicit operator T?(AtomicReference<T> aRef) => aRef.Value;

    /// <summary>
    ///     Implicit conversion operator = allows us to cast any type directly into a <see cref="AtomicReference{T}" />
    ///     instance.
    /// </summary>
    /// <param name="newValue"></param>
    /// <returns></returns>
    public static implicit operator AtomicReference<T>(T newValue) => new(newValue);

    #endregion
}
