using System.Threading;

namespace Sweeper.Core.Atomic
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
        private void AddInternal(int value)
        {
            Interlocked.Add(ref atomicValue, value);
        }
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case AtomicInteger atomicInteger:
                    return atomicInteger.atomicValue == atomicValue;
                case int value:
                    return value == atomicValue;
                default:
                    return false;
            }
        }

        public override int GetHashCode()
        {
            return atomicValue.GetHashCode();
        }

        public static AtomicInteger operator +(AtomicInteger atomicInteger, int value)
        {
            atomicInteger.AddInternal(value);
            return atomicInteger;
        }

        public static AtomicInteger operator +(int value, AtomicInteger atomicInteger)
        {
            atomicInteger.AddInternal(value);
            return atomicInteger;
        }

        public static AtomicInteger operator -(AtomicInteger atomicInteger, int value)
        {
            atomicInteger.AddInternal(-value);
            return atomicInteger;
        }

        public static AtomicInteger operator -(int value, AtomicInteger atomicInteger)
        {
            atomicInteger.AddInternal(-value);
            return atomicInteger;
        }

        public static implicit operator AtomicInteger(int value)
        {
            return new AtomicInteger(value);
        }

        public static implicit operator int(AtomicInteger atomicInteger)
        {
            return atomicInteger.atomicValue;
        }

        public static bool operator ==(AtomicInteger atomicInteger, int value)
        {
            return atomicInteger.atomicValue == value;
        }

        public static bool operator !=(AtomicInteger atomicInteger, int value)
        {
            return !(atomicInteger == value);
        }

        public static bool operator ==(int value, AtomicInteger atomicInteger)
        {
            return atomicInteger.atomicValue == value;
        }

        public static bool operator !=(int value, AtomicInteger atomicInteger)
        {
            return !(value == atomicInteger);
        }
    }
}
