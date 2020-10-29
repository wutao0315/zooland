using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooyard.Core;
using Zooyard.Core.Utils;

namespace Zooyard.Rpc.LoadBalance
{
    public class RoundRobinLoadBalance : AbstractLoadBalance
    {
        public override string Name => NAME;
        public const string NAME = "roundrobin";
        private readonly ConcurrentDictionary<string, AtomicPositiveInteger> _sequences = new ConcurrentDictionary<string, AtomicPositiveInteger>();

        protected override URL DoSelect(IList<URL> urls, IInvocation invocation)
        {
            var key = urls[0].ServiceKey + "." + invocation.MethodInfo.Name;
            int length = urls.Count; // 总个数
            int maxWeight = 0; // 最大权重
            int minWeight = int.MaxValue; // 最小权重
            var invokerToWeightMap = new Dictionary<URL, IntegerWrapper>();
            int weightSum = 0;
            for (int i = 0; i < length; i++)
            {
                int weight = base.GetWeight(urls[i], invocation);
                maxWeight = Math.Max(maxWeight, weight); // 累计最大权重
                minWeight = Math.Min(minWeight, weight); // 累计最小权重
                if (weight > 0)
                {
                    invokerToWeightMap.Add(urls[i], new IntegerWrapper(weight));
                    weightSum += weight;
                }
            }

            if (!_sequences.ContainsKey(key))
            {
                _sequences.GetOrAdd(key, new AtomicPositiveInteger());
            }
            var sequence = _sequences[key];
            
            int currentSequence = sequence.GetAndIncrement();
            if (maxWeight > 0 && minWeight < maxWeight)
            { // 权重不一样
                int mod = currentSequence % weightSum;
                for (int i = 0; i < maxWeight; i++)
                {
                    foreach (var each in invokerToWeightMap)
                    {
                        var k = each.Key;
                        var v = each.Value;
                        if (mod == 0 && v.GetValue() > 0)
                        {
                            return k;
                        }
                        if (v.GetValue() > 0)
                        {
                            v.Decrement();
                            mod--;
                        }
                    }
                }
            }
            // 取模轮循
            return urls[currentSequence % length];
        }

        private sealed class IntegerWrapper
        {
            private int value;

            public IntegerWrapper(int value)
            {
                this.value = value;
            }

            public int GetValue()
            {
                return value;
            }

            public void SetValue(int value)
            {
                this.value = value;
            }

            public void Decrement()
            {
                this.value--;
            }
        }

    }
}
