using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class ListMerger<T> : IMerger<IEnumerable<T>>
    {
        public IEnumerable<T> Merge(params IEnumerable<T>[] items)
        {
            var result = new List<T>();
            foreach (var item in items)
            {
                if (item != null)
                {
                    result.AddRange(item);
                }
            }
            return result;
        }
    }
}
