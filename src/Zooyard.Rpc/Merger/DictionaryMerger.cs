﻿using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class DictionaryMerger<key, value> : IMerger<IDictionary<key, value>>
    {
        public IDictionary<key, value> Merge(params IDictionary<key, value>[] items)
        {
            if (items.Length == 0)
            {
                return null;
            }
            var result = new Dictionary<key, value>();
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
