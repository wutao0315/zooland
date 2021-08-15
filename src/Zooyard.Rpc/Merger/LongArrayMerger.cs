using System;
using Zooyard;

namespace Zooyard.Rpc.Merger
{
    public class LongArrayMerger : IMerger<long[]>
    {
        public long[] Merge(params long[][] items)
        {
            int total = 0;
            foreach (long[] array in items)
            {
                total += array.Length;
            }
            long[] result = new long[total];
            int index = 0;
            foreach (long[] array in items)
            {
                foreach (long item in array)
                {
                    result[index++] = item;
                }
            }
            return result;
        }
    }
}
