using System;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class ByteArrayMerger : IMerger<byte[]>
    {
        public byte[] Merge(params byte[][] items)
        {
            int total = 0;
            foreach (byte[] array in items)
            {
                total += array.Length;
            }
            byte[] result = new byte[total];
            int index = 0;
            foreach (byte[] array in items)
            {
                foreach (byte item in array)
                {
                    result[index++] = item;
                }
            }
            return result;
        }
    }
}
