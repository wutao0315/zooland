using System;
using System.Collections.Generic;
using Zooyard;

namespace Zooyard.Rpc.LoadBalance
{
    public class RandomLoadBalance : AbstractLoadBalance
    {
        public override string Name => NAME;
        public const string NAME = "random";
        private readonly Random random = new Random();

        protected override URL DoSelect(IList<URL> urls, IInvocation invocation)
        {
            int length = urls.Count; // 总个数
            int totalWeight = 0; // 总权重
            var sameWeight = true; // 权重是否都一样
            for (int i = 0; i < length; i++)
            {
                int weight = GetWeight(urls[i], invocation);
                totalWeight += weight; // 累计总权重
                if (sameWeight && i > 0
                        && weight != GetWeight(urls[i - 1], invocation))
                {
                    sameWeight = false; // 计算所有权重是否一样
                }
            }
            if (totalWeight > 0 && !sameWeight)
            {
                // 如果权重不相同且权重大于0则按总权重数随机
                int offset = random.Next(totalWeight);
                // 并确定随机值落在哪个片断上
                for (int i = 0; i < length; i++)
                {
                    offset -= GetWeight(urls[i], invocation);
                    if (offset < 0)
                    {
                        return urls[i];
                    }
                }
            }
            // 如果权重相同或权重为0则均等随机
            return urls[random.Next(length)];
        }
    }
}
