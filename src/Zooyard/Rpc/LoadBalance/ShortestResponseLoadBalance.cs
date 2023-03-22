using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Atomic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Zooyard.Rpc.LoadBalance;

public class ShortestResponseLoadBalance : AbstractLoadBalance
{
    public const string NAME = "shortestresponse";
    public override string Name => NAME;

    private int slidePeriod = 30_000;

    private readonly ConcurrentDictionary<RpcStatus, SlideWindowData> methodMap = new();

    private readonly AtomicBoolean onResetSlideWindow = new(false);

    private long lastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    protected class SlideWindowData
    {

        private long succeededOffset;
        private long succeededElapsedOffset;
        private readonly RpcStatus rpcStatus;

        public SlideWindowData(RpcStatus rpcStatus)
        {
            this.rpcStatus = rpcStatus;
            this.succeededOffset = 0;
            this.succeededElapsedOffset = 0;
        }

        public void Reset()
        {
            this.succeededOffset = rpcStatus.GetSucceeded();
            this.succeededElapsedOffset = rpcStatus.GetSucceededElapsed();
        }

        private long GetSucceededAverageElapsed()
        {
            long succeed = this.rpcStatus.GetSucceeded() - this.succeededOffset;
            if (succeed == 0)
            {
                return 0;
            }
            return (this.rpcStatus.GetSucceededElapsed() - this.succeededElapsedOffset) / succeed;
        }

        public long GetEstimateResponse()
        {
            int active = this.rpcStatus.GetActive() + 1;
            return GetSucceededAverageElapsed() * active;
        }
    }

    protected override URL DoSelect(IList<URL> urls, IInvocation invocation)
    {
        // Number of invokers
        int length = urls.Count();
        // Estimated shortest response time of all invokers
        long shortestResponse = long.MaxValue;
        // The number of invokers having the same estimated shortest response time
        int shortestCount = 0;
        // The index of invokers having the same estimated shortest response time
        int[] shortestIndexes = new int[length];
        // the weight of every invokers
        int[] weights = new int[length];
        // The sum of the warmup weights of all the shortest response  invokers
        int totalWeight = 0;
        // The weight of the first shortest response invokers
        int firstWeight = 0;
        // Every shortest response invoker has the same weight value?
        bool sameWeight = true;

        // Filter out all the shortest response invokers
        for (int i = 0; i < length; i++)
        {
            var invoker = urls[i];
            RpcStatus rpcStatus = RpcStatus.GetStatus(invoker, invocation.MethodInfo.Name);
            SlideWindowData slideWindowData = methodMap.GetOrAdd(rpcStatus, new SlideWindowData(rpcStatus));

            // Calculate the estimated response time from the product of active connections and succeeded average elapsed time.
            long estimateResponse = slideWindowData.GetEstimateResponse();
            int afterWarmup = GetWeight(invoker, invocation);
            weights[i] = afterWarmup;
            // Same as LeastActiveLoadBalance
            if (estimateResponse < shortestResponse)
            {
                shortestResponse = estimateResponse;
                shortestCount = 1;
                shortestIndexes[0] = i;
                totalWeight = afterWarmup;
                firstWeight = afterWarmup;
                sameWeight = true;
            }
            else if (estimateResponse == shortestResponse)
            {
                shortestIndexes[shortestCount++] = i;
                totalWeight += afterWarmup;
                if (sameWeight && i > 0
                    && afterWarmup != firstWeight)
                {
                    sameWeight = false;
                }
            }
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastUpdateTime > slidePeriod
            && onResetSlideWindow.CompareAndSet(false, true))
        {
            _ = Task.Run(() => {
                foreach (var reset in methodMap.Values)
                {
                    lastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    onResetSlideWindow.Value = false;
                }
            });
        }

        if (shortestCount == 1)
        {
            return urls[shortestIndexes[0]];
        }
        if (!sameWeight && totalWeight > 0)
        {
            int offsetWeight = new Random().Next(totalWeight);
            for (int i = 0; i < shortestCount; i++)
            {
                int shortestIndex = shortestIndexes[i];
                offsetWeight -= weights[shortestIndex];
                if (offsetWeight < 0)
                {
                    return urls[shortestIndex];
                }
            }
        }
        return urls[shortestIndexes[new Random().Next(shortestCount)]];
    }
}
