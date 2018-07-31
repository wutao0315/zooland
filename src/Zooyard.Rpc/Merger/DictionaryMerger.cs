using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class DictionaryMerger : IMerger<IDictionary<object, object>>
    {
        public IDictionary<object, object> Merge(params IDictionary<object, object>[] items)
        {
            if (items.Length == 0)
            {
                return null;
            }
            var result = new Dictionary<object, object>();
            foreach (var item in items)
			{
                if (item != null)
                {
                    result.PutAll(item);
                }
            }
            return result;
        }
    }
}
