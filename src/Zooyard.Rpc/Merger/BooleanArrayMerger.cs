using System;
using Zooyard;

namespace Zooyard.Rpc.Merger
{
    public class BooleanArrayMerger : IMerger<bool[]>
    {

        public bool[] Merge(params bool[][] items)
        {
            int totalLen = 0;
            foreach (bool[] array in items)
            {
                totalLen += array.Length;
            }
            bool[] result = new bool[totalLen];
            int index = 0;
            foreach (bool[] array in items)
            {
                foreach (bool item in array)
                {
                    result[index++] = item;
                }
            }
            return result;
        }
    }
}
