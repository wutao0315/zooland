using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Zooyard.Diagnositcs;
//using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

public abstract class AbstractCluster : ICluster
{
    protected readonly ILogger _logger;
    public AbstractCluster(ILogger logger) 
    {
        _logger = logger;
    }

    protected static DiagnosticSource _source = new DiagnosticListener(Constant.DiagnosticListenerName);
    public const string TIMEOUT_KEY = "timeout";
    public const int DEFAULT_TIMEOUT = 1000;
    /// <summary>
    /// 集群时是否启用sticky策略
    /// </summary>
    public const string CLUSTER_STICKY_KEY = "sticky";
    public const string CACHE_KEY = "cache";
    /// <summary>
    /// sticky默认值.
    /// </summary>
    public const bool DEFAULT_CLUSTER_STICKY = false;

    private volatile URL? stickyInvoker = null;
    protected bool availablecheck;

    public abstract string Name { get; }

    /// <summary>
    /// 使用loadbalance选择invoker.
    /// a)先lb选择，如果在selected列表中 或者 不可用且做检验时，进入下一步(重选),否则直接返回
    ///  b)重选验证规则：selected > available .保证重选出的结果尽量不在select中，并且是可用的
    /// </summary>
    /// <param name="loadbalance"></param>
    /// <param name="invocation"></param>
    /// <param name="invokers"></param>
    /// <param name="disabledUrls"></param>
    /// <param name="selected"> 已选过的invoker.注意：输入保证不重复</param>
    /// <returns></returns>
    protected URL Select(ILoadBalance loadbalance, IInvocation invocation, IList<URL>? invokers, IList<BadUrl> disabledUrls, IList<URL>? selected = null)
    {
        if (invokers == null || invokers.Count == 0)
            throw new RpcException("invokers is null or empty");

        string methodName = invocation.MethodInfo.Name;

        var sticky = invokers[0].GetMethodParameter<bool>(methodName, CLUSTER_STICKY_KEY, DEFAULT_CLUSTER_STICKY);
        //ignore overloaded method
        if (invokers != null && stickyInvoker != null && !invokers.Contains(stickyInvoker))
        {
            stickyInvoker = null;
        }
        //ignore cucurrent problem
        if (sticky && stickyInvoker != null && (selected == null || !selected.Contains(stickyInvoker)))
        {
            if (availablecheck && !disabledUrls.Any(w=>w.Url == stickyInvoker))
            {
                return stickyInvoker;
            }
        }

        IList<URL> invokerList = new List<URL>();
        //如果可用集合有多个时过滤隔离集合
        if (invokers?.Count > 1)
        {
            foreach (var item in invokers)
            {
                if (disabledUrls.Any(w => w.Url == item))
                {
                    continue;
                }
                invokerList.Add(item);
            }

            if (invokerList.Count <= 0)
            {
                invokerList = invokers;
            }
        }
        else if (invokers != null)
        {
            invokerList = invokers;
        }

        var invoker = DoSelect(loadbalance, invocation, invokerList, selected);

        if (invoker == null) 
            throw new Exception("invoker is null load balance err");

        if (sticky)
        {
            stickyInvoker = invoker;
        }

        RpcContext.GetContext().SetInvoker(invoker);

        return invoker;

        URL? DoSelect(ILoadBalance loadbalance, IInvocation invocation, IList<URL>? invokers, IList<URL>? selected)
        {
            if (invokers == null || invokers.Count == 0)
                return null;
            if (invokers.Count == 1)
                return invokers[0];
            // 如果只有两个invoker，退化成轮循
            if (invokers.Count == 2 && selected?.Count > 0)
            {
                return selected[0] == invokers[0] ? invokers[1] : invokers[0];
            }
            var invoker = loadbalance.Select(invokers, invocation);

            if (invoker == null)
            {
                return null;
            }

            if ((selected == null || !selected.Contains(invoker)) && !availablecheck) 
            {
                return invoker;
            }

            //如果 selected中包含（优先判断） 或者 不可用&&availablecheck=true 则重试.
            try
            {
                var rinvoker = ReSelect(loadbalance, invocation, invokers, selected, availablecheck);
                if (rinvoker != null)
                {
                    invoker = rinvoker;
                }
                else
                {
                    //看下第一次选的位置，如果不是最后，选+1位置.
                    int index = invokers.IndexOf(invoker);
                    try
                    {
                        //最后在避免碰撞
                        invoker = index < invokers.Count - 1 ? invokers[index + 1] : invoker;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, e.Message + " may because invokers list dynamic change, ignore.");
                    }
                }
            }
            catch (Exception t)
            {
                _logger.LogError(t, $"clustor relselect fail reason is :{t.Message} if can not slove ,you can set cluster.availablecheck=false in url");
            }

            return invoker;
        }

        // 重选，先从非selected的列表中选择，没有在从selected列表中选择.
        URL? ReSelect(ILoadBalance loadbalance, IInvocation invocation, IList<URL> invokers, IList<URL>? selected, bool availablecheck)
        {
            //预先分配一个，这个列表是一定会用到的.
            var reselectInvokers = new List<URL>(invokers.Count > 1 ? (invokers.Count - 1) : invokers.Count);

            //先从非select中选
            if (availablecheck)
            { 
                //选isAvailable 的非select
                foreach (var invoker in invokers)
                {
                    if (selected == null || !selected.Contains(invoker))
                    {
                        reselectInvokers.Add(invoker);
                    }
                }
                if (reselectInvokers.Count > 0)
                {
                    return loadbalance.Select(reselectInvokers, invocation);
                }
            }
            else
            { 
                //选全部非select
                foreach (var invoker in invokers)
                {
                    if (selected == null || !selected.Contains(invoker))
                    {
                        reselectInvokers.Add(invoker);
                    }
                }
                if (reselectInvokers.Count > 0)
                {
                    return loadbalance.Select(reselectInvokers, invocation);
                }
            }
            //最后从select中选可用的. 
            if (selected != null)
            {
                foreach (var invoker in selected)
                {
                    if (!reselectInvokers.Contains(invoker))
                    {
                        reselectInvokers.Add(invoker);
                    }
                }
            }
            if (reselectInvokers.Count > 0)
            {
                return loadbalance.Select(reselectInvokers, invocation);
            }
            return null;
        }

    }

    protected void CheckInvokers(IList<URL>? invokers, IInvocation invocation, URL address)
    {
        if (invokers == null || invokers.Count == 0)
        {
            throw new RpcException("Failed to invoke the method "
                    + invocation.MethodInfo.Name + " in the service " + invocation.TargetType.Name
                    + ". No provider available for the service " + invocation.ServiceName
                    + " from registry " + address
                    //+ " on the consumer " + NetUtils.getLocalHost()
                    + " using the zooyard version " + invocation.Version
                    + ". Please check if the providers have been started and registered.");
        }
    }

    public virtual async Task<IClusterResult<T>> Invoke<T>(IClientPool pool, 
        ILoadBalance loadbalance,
        URL address, 
        IList<URL> urls,
        IList<BadUrl> disabledUrls,
        IInvocation invocation) 
    {
        var result = await this.DoInvoke<T>(pool, loadbalance, address, urls, disabledUrls, invocation);
        return result;
    }
    protected abstract Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool,
        ILoadBalance loadbalance, 
        URL address, 
        IList<URL> urls, 
        IList<BadUrl> disabledUrls,
        IInvocation invocation);
}
