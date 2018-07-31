using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq.Expressions;

namespace Zooyard.Core.Atomic
{
    public class AtomicNumber<T>
    {
        T atomicValue;
        //private static readonly object LOCK = new object();
        public AtomicNumber()
        {

        }
        public AtomicNumber(T originalValue)
        {
            this.atomicValue = originalValue;
        }

        public T IncrementAndGet() 
        {
            //Interlocked.Increment()
            Volatile
            lock (valueData) 
            {
                valueData = Operator<T>.Inc(valueData);
                return valueData;
                //decimal data = (decimal)valueData.ChangeType(typeof(decimal));
                //data++;
                //this.valueData = (T)data.ChangeType(typeof(T));
                //return valueData;
            }
        }
        public T GetAndIncrement() 
        {
            lock (valueData)
            {
                if (valueData is int) 
                { 
                    valueData=(Convert.ToInt32(valueData)==int.MaxValue?(T)int.MinValue.ChangeType(typeof(T)):valueData);
                }
                else if(valueData is long)
                {
                    valueData=(Convert.ToInt64(valueData)==long.MaxValue?(T)long.MinValue.ChangeType(typeof(T)):valueData);
                }
                else if(valueData is float){
                    valueData = (Convert.ToSingle(valueData) == float.MaxValue ? (T)float.MinValue.ChangeType(typeof(T)) : valueData);
                }
                else if (valueData is double)
                {
                    valueData = (Convert.ToDouble(valueData) == double.MaxValue ? (T)double.MinValue.ChangeType(typeof(T)) : valueData);
                }
                else if (valueData is decimal)
                {
                    valueData = (Convert.ToDecimal(valueData) == decimal.MaxValue ? (T)decimal.MinValue.ChangeType(typeof(T)) : valueData);
                }
                else if (valueData is short)
                {
                    valueData = (Convert.ToInt16(valueData) == short.MaxValue ? (T)short.MinValue.ChangeType(typeof(T)) : valueData);
                }
                valueData = Operator<T>.Inc(valueData);
                return valueData;
            }
        }
        public T DecrementAndGet() 
        {
            lock (valueData)
            {
                this.valueData = Operator<T>.Dec(this.valueData);
                return valueData;
                //decimal data = (decimal)valueData.ChangeType(typeof(decimal));
                //data--;
                //this.valueData = (T)data.ChangeType(typeof(T));
                //return valueData;
            }
        }

        /// <summary>
        ///     If <see cref="Value" /> equals <see cref="expected" />, then set the Value to
        ///     <see cref="newValue" />.
        ///     Returns true if <see cref="newValue" /> was set, false otherwise.
        /// </summary>
        public bool CompareAndSet(T expected, T newValue) => Interlocked.CompareExchange(ref this.atomicValue, newValue, expected) == expected;

        public T AddAndGet(T elapsed) 
        {
           Interlocked.Increment()
            lock (valueData)
            {
                valueData= Operator<T>.Add(valueData, elapsed);
                return valueData;
            }
        }
        public T GetAndSet(T elapsed) 
        {
            lock (valueData)
            {
                T result = valueData;
                valueData = elapsed;
                return result;
            }
        }
        
        public T Value 
        {
            get 
            {
                lock (valueData) 
                {
                    return valueData;
                }
            }
            set 
            {
                lock (valueData)
                {
                    valueData=value;
                }
            }
        }

    }

    public static class Operator<T>
    {
        private static readonly Func<T, T, T> add;
        private static readonly Func<T, T> inc;
        private static readonly Func<T, T> dec;
        private static readonly Func<T, T, bool> equal;
        private static readonly Func<T, T, bool> lessThan;
        private static readonly Func<T, T, bool> lessOrEqualThan;
        private static readonly Func<T, T, bool> greaterThan;
        private static readonly Func<T, T, bool> greaterOrEqualThan;
        public static T Add(T x, T y)
        {
            return add(x, y);
        }
        public static bool Equal(T x, T y)
        {
            return equal(x, y);
        }
        
        public static bool LessThan(T x,T y) 
        {
            return lessThan(x,y);
        }

        public static bool GreaterThan(T x, T y)
        {
            return greaterThan(x, y);
        }

        public static T Inc(T x)
        {
            return inc(x);
        }

        public static T Dec(T x)
        {
            return dec(x);
        }


        static Operator()
        {
            var x = Expression.Parameter(typeof(T), "x");
            var y = Expression.Parameter(typeof(T), "y");

            var addBody = Expression.Add(x, y);
            add = Expression.Lambda<Func<T, T, T>>(
                addBody, x, y).Compile();

            var equalBody = Expression.Equal(x, y);
            equal = Expression.Lambda<Func<T, T, bool>>(
                equalBody, x, y).Compile();

            var lessBody = Expression.LessThan(x,y);
            lessThan = Expression.Lambda<Func<T, T, bool>>(
                lessBody, x, y).Compile();

            var lessOrEqualBody = Expression.LessThanOrEqual(x, y);
            lessOrEqualThan = Expression.Lambda<Func<T, T, bool>>(
                lessOrEqualBody, x, y).Compile();


            var greaterBody=Expression.GreaterThan(x, y);
            greaterThan = Expression.Lambda<Func<T, T, bool>>(
                greaterBody, x, y).Compile();

            var greaterOrEqualBody = Expression.GreaterThanOrEqual(x, y);
            greaterOrEqualThan = Expression.Lambda<Func<T, T, bool>>(
                greaterOrEqualBody, x, y).Compile();

            var incBody = Expression.Increment(x);

            inc = Expression.Lambda<Func<T, T>>(
                incBody, x).Compile();

            var decBody = Expression.Decrement(x);

            dec = Expression.Lambda<Func<T, T>>(
                decBody, x).Compile();

        }
    }


}
