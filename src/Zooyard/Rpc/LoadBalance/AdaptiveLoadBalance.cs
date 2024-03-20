using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Zooyard.Atomic;

namespace Zooyard.Rpc.LoadBalance;

public class AdaptiveLoadBalance : AbstractLoadBalance
{
    public override string Name => NAME;
    public const string NAME = "adaptive";

    public const string ADAPTIVE_LOADBALANCE_ATTACHMENT_KEY = "lb_adaptive";
    public const string ADAPTIVE_LOADBALANCE_START_TIME = "adaptive_startTime";
    public const string ADAPTIVE = "adaptive";
    //default key
    private string attachmentKey = "mem,load";

    private readonly AdaptiveMetrics _adaptiveMetrics;
    public AdaptiveLoadBalance(AdaptiveMetrics adaptiveMetrics)
    {
        _adaptiveMetrics = adaptiveMetrics;
    }

    //public AdaptiveLoadBalance(ApplicationModel scopeModel)
    //{
    //    adaptiveMetrics = scopeModel.getBeanFactory().getBean(AdaptiveMetrics.class);
    //}

    protected override URL DoSelect(IList<URL> invokers, IInvocation invocation)
    {
        var invoker = SelectByP2C(invokers, invocation);

        invocation.SetAttachment(ADAPTIVE_LOADBALANCE_ATTACHMENT_KEY, attachmentKey);
        long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        invocation.GetAttributes()[ADAPTIVE_LOADBALANCE_START_TIME] = startTime;
        invocation.GetAttributes()[CommonConstants.LOADBALANCE_KEY] = ADAPTIVE;
        _adaptiveMetrics.AddConsumerReq(GetServiceKey(invoker, invocation));
        _adaptiveMetrics.SetPickTime(GetServiceKey(invoker, invocation), startTime);

        return invoker;
    }

    private URL SelectByP2C(IList<URL> invokers, IInvocation invocation)
    {
        int length = invokers.Count;
        if (length == 1)
        {
            return invokers[0];
        }

        if (length == 2)
        {
            return ChooseLowLoadInvoker(invokers[0], invokers[1], invocation);
        }

        int pos1 = ThreadLocalRandom.Current().Next(length);
        int pos2 = ThreadLocalRandom.Current().Next(length - 1);
        if (pos2 >= pos1)
        {
            pos2 = pos2 + 1;
        }

        return ChooseLowLoadInvoker(invokers[pos1], invokers[pos2], invocation);
    }

    private string GetServiceKey(URL invoker, IInvocation invocation)
    {

        if (invocation.GetAttributes().TryGetValue(invoker, out var key)
                && key is string keyStr
                && !string.IsNullOrWhiteSpace(keyStr))
        {
            return keyStr;
        }

        keyStr = buildServiceKey(invoker, invocation);
        invocation.GetAttributes()[invoker] = keyStr;
        return keyStr;
    }

    private string buildServiceKey(URL invoker, IInvocation invocation)
    {
        var sb = new StringBuilder(128);
        sb.Append(invoker.Address).Append(':').Append(invocation.ProtocolServiceKey);
        return sb.ToString();
    }

    private int GetTimeout(URL invoker, IInvocation invocation)
    {
        URL url = invoker;
        string methodName = invocation.MethodInfo.Name;

        return getTimeout(url, methodName, RpcContext.GetContext(), invocation, CommonConstants.DEFAULT_TIMEOUT);

        int getTimeout(URL url, string methodName, RpcContext context, IInvocation invocation, int defaultTimeout)
        {
            var timeout = defaultTimeout;
            var timeoutFromContext = context.GetAttachment(CommonConstants.TIMEOUT_KEY);
            var timeoutFromInvocation = invocation.GetAttachment(CommonConstants.TIMEOUT_KEY);

            if (timeoutFromContext != null)
            {
                timeout = convertToNumber(timeoutFromContext, defaultTimeout);
            }
            else if (timeoutFromInvocation != null)
            {
                timeout = convertToNumber(timeoutFromInvocation, defaultTimeout);
            }
            else if (url != null)
            {
                timeout = url.GetMethodPositiveParameter(methodName, CommonConstants.TIMEOUT_KEY, defaultTimeout);
            }
            return timeout;
        }

        int convertToNumber(object obj, int defaultTimeout)
        {
            int? timeout = convertToNumberObj(obj);
            return timeout == null ? defaultTimeout : timeout.Value;
        }

        int? convertToNumberObj(object obj)
        {
            int? timeout = null;
            try
            {
                if (obj is string objStr)
                {
                    timeout = int.Parse(objStr);
                }
                else
                {
                    timeout = int.Parse(obj.ToString()!);
                }
            }
            catch
            {
                // ignore
            }
            return timeout;
        }
    }

    private URL ChooseLowLoadInvoker(URL invoker1, URL invoker2, IInvocation invocation)
    {
        int weight1 = GetWeight(invoker1, invocation);
        int weight2 = GetWeight(invoker2, invocation);
        int timeout1 = GetTimeout(invoker2, invocation);
        int timeout2 = GetTimeout(invoker2, invocation);
        long load1 = BitConverter.DoubleToInt64Bits(_adaptiveMetrics.GetLoad(GetServiceKey(invoker1, invocation), weight1, timeout1));
        long load2 = BitConverter.DoubleToInt64Bits(_adaptiveMetrics.GetLoad(GetServiceKey(invoker2, invocation), weight2, timeout2));

        if (load1 == load2)
        {
            // The sum of weights
            int totalWeight = weight1 + weight2;
            if (totalWeight > 0)
            {
                int offset = ThreadLocalRandom.Current().Next(totalWeight);
                if (offset < weight1)
                {
                    return invoker1;
                }
                return invoker2;
            }
            return ThreadLocalRandom.Current().Next(2) == 0 ? invoker1 : invoker2;
        }
        return load1 > load2 ? invoker2 : invoker1;
    }
}


public sealed record AdaptiveMetrics
{
    private readonly ConcurrentDictionary<string, AdaptiveMetrics> _metricsStatistics = new();
    private long currentProviderTime = 0;
    private double providerCPULoad = 0;
    private long lastLatency = 0;
    private long currentTime = 0;

    //Allow some time disorder
    private long pickTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private double beta = 0.5;
    private readonly AtomicLong _consumerReq = new();
    private readonly AtomicLong _consumerSuccess = new();
    private readonly AtomicLong _errorReq = new();
    private double ewma = 0;

    public double GetLoad(string idKey, int weight, int timeout)
    {
        AdaptiveMetrics metrics = GetStatus(idKey);

        //If the time more than 2 times, mandatory selected
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - metrics.pickTime > timeout * 2)
        {
            return 0;
        }

        if (metrics.currentTime > 0)
        {
            int multiple = (int)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - metrics.currentTime) / timeout + 1);
            if (multiple > 0)
            {
                if (metrics.currentProviderTime == metrics.currentTime)
                {
                    //penalty value
                    metrics.lastLatency = timeout * 2L;
                }
                else
                {
                    metrics.lastLatency = metrics.lastLatency >> multiple;
                }
                metrics.ewma = metrics.beta * metrics.ewma + (1 - metrics.beta) * metrics.lastLatency;
                metrics.currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        long inflight = metrics._consumerReq.Value - metrics._consumerSuccess.Value - metrics._errorReq.Value;
        return metrics.providerCPULoad * (Math.Sqrt(metrics.ewma) + 1) * (inflight + 1) / ((((double)metrics._consumerSuccess.Value / (double)(metrics._consumerReq.Value + 1)) * weight) + 1);
    }

    public AdaptiveMetrics GetStatus(string idKey)
    {
        return _metricsStatistics.GetOrAdd(idKey, new AdaptiveMetrics());
    }

    public void AddConsumerReq(string idKey)
    {
        AdaptiveMetrics metrics = GetStatus(idKey);
        metrics._consumerReq.IncrementAndGet();
    }

    public void AddConsumerSuccess(String idKey)
    {
        AdaptiveMetrics metrics = GetStatus(idKey);
        metrics._consumerSuccess.IncrementAndGet();
    }

    public void AddErrorReq(String idKey)
    {
        AdaptiveMetrics metrics = GetStatus(idKey);
        metrics._errorReq.IncrementAndGet();
    }

    public void SetPickTime(String idKey, long time)
    {
        AdaptiveMetrics metrics = GetStatus(idKey);
        metrics.pickTime = time;
    }


    public void SetProviderMetrics(String idKey, IDictionary<string, string> metricsMap)
    {
        AdaptiveMetrics metrics = GetStatus(idKey);

        long serviceTime = 0;
        if (metricsMap.TryGetValue("curTime", out var curTimeStr) && long.TryParse(curTimeStr, out var curTime))
        {
            serviceTime = curTime;
        }

        //If server time is less than the current time, discard
        if (metrics.currentProviderTime > serviceTime)
        {
            return;
        }

        metrics.currentProviderTime = serviceTime;
        metrics.currentTime = serviceTime;
        metrics.providerCPULoad = 0d;
        if (metricsMap.TryGetValue("load", out var loadStr) && double.TryParse(loadStr, out var load))
        {
            metrics.providerCPULoad = load;
        }
        metrics.lastLatency = 0;
        if (metricsMap.TryGetValue("rt", out var rtStr) && long.TryParse(rtStr, out var rt))
        {
            metrics.lastLatency = rt;
        }

        metrics.beta = 0.5;
        //Vt =  β * Vt-1 + (1 -  β ) * θt
        metrics.ewma = metrics.beta * metrics.ewma + (1 - metrics.beta) * metrics.lastLatency;

    }
}

public record ThreadLocalRandom
{
    private static readonly AsyncLocal<Random> LOCAL = new();

    public static Random Current()
    {
        LOCAL.Value ??= new Random();
        return LOCAL.Value;
    }
}