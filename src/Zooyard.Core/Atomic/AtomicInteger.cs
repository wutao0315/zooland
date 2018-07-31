using System.Threading;

namespace Zooyard.Core.Atomic
{
    public sealed class AtomicInteger
    {
        int atomicValue;
        public AtomicInteger()
        {

        }
        public AtomicInteger(int originalValue)
        {
            this.atomicValue = originalValue;
        }
        /// <summary>
        ///     The current value of this <see cref="AtomicInteger" />
        /// </summary>
        public int Value
        {
            get { return Volatile.Read(ref this.atomicValue); }
            set { Volatile.Write(ref this.atomicValue, value); }
        }
        public int IncrementAndGet()
        {
            return Interlocked.Increment(ref this.atomicValue);
        }

        public int DecrementAndGet()
        {
            return Interlocked.Decrement(ref this.atomicValue);
        }

        public int AddAndGet(int elapsed)
        {
            return Interlocked.Add(ref this.atomicValue, elapsed);
        }
        public int GetAndSet(int elapsed)
        {
            return Interlocked.Exchange(ref this.atomicValue, elapsed);
        }
        public bool CompareAndSet(int expected, int newValue) => Interlocked.CompareExchange(ref this.atomicValue, newValue, expected) == expected;
    }
}
