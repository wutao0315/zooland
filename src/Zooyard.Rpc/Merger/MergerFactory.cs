using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Merger
{
    public class MergerFactory
    {
        public static IMerger GetMerger(Type returnType, IDictionary<Type, IMerger> mergerCache)
        {
            IMerger result;
            if (returnType.IsArray)
            {
                var type=returnType.GetElementType();
                if (mergerCache.ContainsKey(type))
                {
                    result = mergerCache[type];
                }
                else {
                    result = mergerCache[type];
                }
                if (result == null && !type.IsPrimitive)
                {
                    result = ArrayMerger.INSTANCE;
                }
            }
            else
            {
                result = mergerCache[returnType];
                if (result == null)
                {
                    result = mergerCache[returnType];
                }
            }
            return result;
        }
    }
}
