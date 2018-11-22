using System;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class FloatArrayMerger : IMerger<float[]>
    {
        public float[] Merge(params float[][] items)
        {
            int total = 0;
            foreach (float[] array in items)
            {
                total += array.Length;
            }
            float[] result = new float[total];
            int index = 0;
            foreach (float[] array in items)
            {
                foreach (float item in array)
                {
                    result[index++] = item;
                }
            }
            return result;
        }
    }
}
