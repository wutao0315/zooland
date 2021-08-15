using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Zooyard.Atomic
{
    public sealed class AtomicBoolean
    {
        private int _value;
        private const int False = 0;
        private const int True = 1;

        public AtomicBoolean() { }
        public AtomicBoolean(bool originalValue)
        {
            _value = originalValue ? True : False;
        }
        /// <summary>
        ///     The current value of this <see cref="AtomicBoolean" />
        /// </summary>
        public bool Value
        {
            get { return ReadCompilerOnlyFence(); }
            set { WriteCompilerOnlyFence(value); }
        }
        public bool ReadUnfenced()
        {
            return ToBool(_value);
        }

        public bool ReadAcquireFence()
        {
            var value = ToBool(_value);
            Thread.MemoryBarrier();
            return value;
        }

        public bool ReadFullFence()
        {
            var value = ToBool(_value);
            Thread.MemoryBarrier();
            return value;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public bool ReadCompilerOnlyFence()
        {
            return ToBool(_value);
        }

        public void WriteReleaseFence(bool newValue)
        {
            var newValueInt = ToInt(newValue);
            Thread.MemoryBarrier();
            _value = newValueInt;
        }

        public void WriteFullFence(bool newValue)
        {
            var newValueInt = ToInt(newValue);
            Thread.MemoryBarrier();
            _value = newValueInt;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void WriteCompilerOnlyFence(bool newValue)
        {
            _value = ToInt(newValue);
        }

        public void WriteUnfenced(bool newValue)
        {
            _value = ToInt(newValue);
        }

        public bool AtomicCompareExchange(bool newValue, bool comparand)
        {
            var newValueInt = ToInt(newValue);
            var comparandInt = ToInt(comparand);

            return Interlocked.CompareExchange(ref _value, newValueInt, comparandInt) == comparandInt;
        }

        public bool AtomicExchange(bool newValue)
        {
            var newValueInt = ToInt(newValue);
            var originalValue = Interlocked.Exchange(ref _value, newValueInt);
            return ToBool(originalValue);
        }

        public override string ToString()
        {
            var value = ReadFullFence();
            return value.ToString();
        }

        private static bool ToBool(int value)
        {
            if (value != False && value != True)
            {
                throw new ArgumentOutOfRangeException("value");
            }

            return value == True;
        }

        private static int ToInt(bool value)
        {
            return value ? True : False;
        }

        public bool CompareAndSet(bool comparand, bool newValue)
        {
            var newValueInt = ToInt(newValue);
            var comparandInt = ToInt(comparand);

            return Interlocked.CompareExchange(ref _value, newValueInt, comparandInt) == comparandInt;
        }
    }
}
