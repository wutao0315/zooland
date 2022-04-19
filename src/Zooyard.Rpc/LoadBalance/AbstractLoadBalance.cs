namespace Zooyard.Rpc.LoadBalance;

public abstract class AbstractLoadBalance : ILoadBalance
{
    public const string WARMUP_KEY = "warmup";
    public const int DEFAULT_WARMUP = 10 * 60 * 1000;
    public const string WEIGHT_KEY = "weight"; 
    public const int DEFAULT_WEIGHT = 100;
    public const string REMOTE_TIMESTAMP_KEY = "remote.timestamp";

    public virtual string Name { get; }
    
    public URL Select(IList<URL> urls, IInvocation invocation)
    {
        if (urls == null || urls.Count == 0)
        {
            return null;
        }

        if (urls.Count == 1)
        {
            return urls[0];
        } 

        return DoSelect(urls, invocation);

    }

    protected abstract URL DoSelect(IList<URL> urls, IInvocation invocation);

    protected int GetWeight(URL url, IInvocation invocation)
    {
        int weight = url.GetMethodParameter(invocation.MethodInfo.Name, WEIGHT_KEY, DEFAULT_WEIGHT);
        if (weight > 0)
        {
            long timestamp = url.GetParameter(REMOTE_TIMESTAMP_KEY, 0L);
            if (timestamp > 0L)
            {
                int uptime = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp);
                int warmup = url.GetParameter(WARMUP_KEY, DEFAULT_WARMUP);
                if (uptime > 0 && uptime < warmup)
                {
                    weight = CalculateWarmupWeight(uptime, warmup, weight);
                }
            }
        }
        return weight;
    }
    int CalculateWarmupWeight(int uptime, int warmup, int weight)
    {
        int ww = (int)(uptime / (warmup / weight));
        return ww < 1 ? 1 : (ww > weight ? weight : ww);
    }
}
