using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Core.Atomic
{
    public class AtomicList<T>:ICollection<T>
    {
        private List<T> valueData = new List<T>();
        //private static readonly object LOCK = new object();

        public void Add(T item) 
        {
            lock (valueData)
            {
                valueData.Add(item);
            }
        }
        public void AddRange(IEnumerable<T> item)
        {
            lock (valueData)
            {
                valueData.AddRange(item);
            }
        }
        public bool Remove(T item) 
        {
            lock (valueData)
            {
                return valueData.Remove(item);
            }
        }
        public void RemoveAt(int index) 
        {
            lock (valueData)
            {
                valueData.RemoveAt(index);
            }
        }
        public void RemoveAll(Predicate<T> match) 
        {
            lock (valueData)
            {
                valueData.RemoveAll(match);
            }
        }
        public void RemoveRange(int index, int count)
        {
            lock (valueData)
            {
                valueData.RemoveRange(index, count);
            }
        }
        public T this[int index] 
        {
            get{
                lock (valueData)
                {
                    return valueData[index];
                }
            }
            set 
            {
                lock (valueData)
                {
                    valueData[index]=value;
                }
            }
        }



        public void Clear()
        {
            lock (valueData) 
            {
                valueData.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (valueData)
            {
                return valueData.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (valueData)
            {
                valueData.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get {
                lock (valueData)
                {
                    return valueData.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (valueData)
            {
                return valueData.GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (valueData)
            {
                return valueData.GetEnumerator();
            }
        }
    }
}
