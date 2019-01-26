using System.Threading;

namespace Zooyard.Core.Atomic
{
    public sealed class AtomicBool
    {
        bool atomicValue;
        public AtomicBool() { }
        public AtomicBool(bool originalValue)
        {
            this.atomicValue = originalValue;
        }
        /// <summary>
        ///     The current value of this <see cref="AtomicBool" />
        /// </summary>
        public bool Value
        {
            get { return Volatile.Read(ref this.atomicValue); }
            set { Volatile.Write(ref this.atomicValue, value); }
        }
    }
}
