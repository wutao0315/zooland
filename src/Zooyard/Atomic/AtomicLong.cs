using System.Threading;

namespace Zooyard.Atomic
{
    public sealed class AtomicLong
    {
        long atomicValue;
        public AtomicLong() { }
        public AtomicLong(long originalValue)
        {
            this.atomicValue = originalValue;
        }
        /// <summary>
        ///     The current value of this <see cref="AtomicLong" />
        /// </summary>
        public long Value
        {
            get { return Volatile.Read(ref this.atomicValue); }
            set { Volatile.Write(ref this.atomicValue, value); }
        }

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref this.atomicValue);
        }
        public long DecrementAndGet()
        {
            return Interlocked.Decrement(ref this.atomicValue);
        }
        public long AddAndGet(long elapsed)
        {
            return Interlocked.Add(ref this.atomicValue, elapsed);
        }
        public long GetAndSet(long elapsed)
        {
            return Interlocked.Exchange(ref this.atomicValue, elapsed);
        }
        public bool CompareAndSet(long expected, long newValue) => Interlocked.CompareExchange(ref this.atomicValue, newValue, expected) == expected;
    }
}
