using System;
using System.Linq;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class ArrayMerger : IMerger<object[]>
    {
        public static ArrayMerger INSTANCE = new ArrayMerger();
        
        public object[] Merge(params object[][] items)
        {
            if (items.Length == 0)
            {
                return null;
            }
            int totalLen = 0;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item != null && item.GetType().IsArray)
                {
                    totalLen += item.Length;
                }
                else
                {
                    throw new ArgumentException($"{i + 1}th argument is not an array");
                }
            }

            if (totalLen == 0)
            {
                return null;
            }

            var type = items[0].GetType().GetElementType();

            var result = Array.CreateInstance(type, totalLen);
            int index = 0;
            foreach (var array in items)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    result.SetValue(array.GetValue(i), index++);
                }
            }

            return (object[])result;
        }
    }
}
