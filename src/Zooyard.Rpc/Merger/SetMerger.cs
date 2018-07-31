using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class SetMerger : IMerger<ISet<object>>
    {
        public ISet<object> Merge(params ISet<object>[] items)
        {
            var result = new HashSet<object>();
            
            foreach (var item in items)
            {
                if (item != null)
                {
                    foreach (var i in item)
                    {
                        result.Add(i);
                    }
                }
            }

            return result;
        }
    }
}
