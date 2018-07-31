using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class ListMerger : IMerger<IList<object>>
    {
        public IList<object> Merge(params IList<object>[] items)
        {
            var result = new List<object>();
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
