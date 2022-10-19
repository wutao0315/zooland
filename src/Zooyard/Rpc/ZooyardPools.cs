using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Zooyard.Logging;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;
using Zooyard.Utils;

namespace Zooyard.Rpc;

/// <summary>
/// Singleton object manage pools
/// </summary>
public class ZooyardPools : IZooyardPools
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ZooyardPools));

    public const string CACHE_KEY = "cache";
    public const string CLUSTER_KEY = "cluster";
    public const string LOADBANCE_KEY = "loadbance";
    public const string CYCLE_PERIOD_KEY = "cycle";
    public const int DEFAULT_CYCLE_PERIOD = 60 * 1000;
    public const string OVER_TIME_KEY = "overtime";
    public const int DEFAULT_OVER_TIME = 5;
    public const string RECOVERY_PERIOD_KEY = "recovery";
    public const int DEFAULT_RECOVERY_PERIOD = 6 * 1000;
    public const string RECOVERY_TIME_KEY = "recoverytime";
    public const int DEFAULT_RECOVERY_TIME = 5;
    public const string APP_KEY = "app";
    /// <summary>
    /// address
    /// if address's protocol is 'registry', the urls will request register center for all urls
    /// other way is connection directly address
    /// </summary>
    public URL Address { get; private set; }
    /// <summary>
    /// good service url list
    /// </summary>
    public ConcurrentDictionary<string, List<URL>> Urls { get; init; }
    /// <summary>
    /// bad service url list
    /// </summary>
    public ConcurrentDictionary<string, List<BadUrl>> BadUrls { get; init; }
    /// <summary>
    /// 注册中心的配置
    /// </summary>
    private readonly IOptionsMonitor<ZooyardOption> _clients;
    /// <summary>
    /// the service pools
    /// key ApplicationName,
    /// value diff version of client pool
    /// </summary>
    public ConcurrentDictionary<string, IClientPool> Pools { get; init; }
    /// <summary>
    /// loadbalance
    /// </summary>
    public ConcurrentDictionary<string, ILoadBalance> LoadBalances { get; init; }
    /// <summary>
    /// cluster
    /// </summary>
    public ConcurrentDictionary<string, ICluster> Clusters { get; init; }
    /// <summary>
    /// cache
    /// </summary>
    public ConcurrentDictionary<string, ICache> Caches { get; init; }
    /// <summary>
    /// 计时器用于处理过期的链接和链接池
    /// </summary>
    private System.Timers.Timer? cycleTimer;
    /// <summary>
    /// 计时器用于处理隔离区域自动恢复到正常区域
    /// </summary>
    private System.Timers.Timer? recoveryTimer;
    /// <summary>
    /// threed lock
    /// </summary>		
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pools"></param>
    public ZooyardPools(IDictionary<string, IClientPool> pools,
        IDictionary<string, ILoadBalance> loadbalances,
        IDictionary<string, ICluster> clusters,
        IDictionary<string, Type> caches,
        IOptionsMonitor<ZooyardOption> clients)
    {
        this.Pools = new ConcurrentDictionary<string, IClientPool>(pools);
        this.LoadBalances = new ConcurrentDictionary<string, ILoadBalance>(loadbalances);
        this.Clusters = new ConcurrentDictionary<string, ICluster>(clusters);
        
        this.Address = URL.ValueOf(string.IsNullOrWhiteSpace(clients.CurrentValue.RegisterUrl)? "zooyard://127.0.0.1" : clients.CurrentValue.RegisterUrl);
        
        this.Urls = new ConcurrentDictionary<string, List<URL>>();
        this.BadUrls = new ConcurrentDictionary<string, List<BadUrl>>();
        ////参数
        //foreach (var item in clients.CurrentValue.Clients)
        //{
        //    if (string.IsNullOrWhiteSpace(item.Value.Service.FullName))
        //    {
        //        continue;
        //    }
        //    var list = item.Urls.Select(w => URL.ValueOf(w).AddParameterIfAbsent("interface", item.Service.FullName)).ToList();
        //    this.Urls.TryAdd(item.Service.FullName, list);
        //}

        this.Caches = new ConcurrentDictionary<string, ICache>();
        foreach (var cache in caches)
        {
            var ctr = cache.Value.GetConstructor(new Type[] { typeof(URL) });
            if (ctr != null 
                && ctr.Invoke(new object[] { this.Address }) is ICache value)
            {
                this.Caches.TryAdd(cache.Key, value);
            }
            else 
            {
                var ctrEmpty = cache.Value.GetConstructor(Array.Empty<Type>());
                if (ctrEmpty != null 
                    && ctrEmpty.Invoke(Array.Empty<object>()) is ICache val)
                {
                    this.Caches.TryAdd(cache.Key, val);
                }
            }
        }

        Init();

        _clients = clients;
        _clients.OnChange(OnChanged);
    }

    private void OnChanged(ZooyardOption value, string name)
    {
        Logger().LogInformation($"{name} has changed:{ value}");
        Console.WriteLine($"{name} has changed:{ value}");

        //this.Address = URL.ValueOf(value.RegisterUrl);

        //foreach (var item in value.Clients)
        //{
        //    var list = item.Value.Urls.Select(w => URL.ValueOf(w).AddParameterIfAbsent("interface", item.Value.Service.FullName)).ToList();
        //    //优先移除被隔离了的URL
        //    if (this.BadUrls.ContainsKey(item.Key))
        //    {
        //        var removeUrls = new List<BadUrl>();
        //        foreach (var badUrl in this.BadUrls[item.Key])
        //        {
        //            var exitsUrl = list.FirstOrDefault(w => w.ToIdentityString() == badUrl.Url.ToIdentityString());
        //            if (exitsUrl == null)
        //            {
        //                removeUrls.Add(badUrl);
        //            }
        //        }
        //        foreach (var url in removeUrls)
        //        {
        //            this.BadUrls[item.Key].Remove(url);
        //        }
        //    }

        //    if (this.Urls.ContainsKey(item.Key))
        //    {
        //        //移除注销的提供者
        //        var removeUrls = new List<URL>();
        //        foreach (var url in this.Urls[item.Key])
        //        {
        //            var exitsUrl = list.FirstOrDefault(w => w.ToIdentityString() == url.ToIdentityString());
        //            if (exitsUrl == null)
        //            {
        //                removeUrls.Add(url);
        //            }
        //        }
        //        foreach (var url in removeUrls)
        //        {
        //            this.Urls[item.Key].Remove(url);
        //        }

        //        //发现新的提供者
        //        foreach (var i in list)
        //        {
        //            URL? exitsUrl = null;
        //            if (this.Urls.TryGetValue(item.Key, out List<URL>? urlList)) 
        //            {
        //                exitsUrl = urlList.FirstOrDefault(w => w.ToIdentityString() == i.ToIdentityString());
        //            }
        //            BadUrl? exitsBadUrl = null;
        //            if (BadUrls.TryGetValue(item.Key, out List<BadUrl>? badUrlList)) 
        //            {
        //                badUrlList.FirstOrDefault(w => w.Url.ToIdentityString() == i.ToIdentityString());
        //            }
        //            if (exitsUrl == null && exitsBadUrl == null)
        //            {
        //                this.Urls[item.Key].Add(i);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        this.Urls.TryAdd(item.Key, list);
        //    }
        //}
    }
    /// <summary>
    /// 初始化调用
    /// </summary>
    public void Init()
    {
        // 定时或者在接收到推送的消息后  主动-维护Pools集合
        var internalCycle = this.Address.GetParameter(CYCLE_PERIOD_KEY, DEFAULT_CYCLE_PERIOD);

        cycleTimer = new System.Timers.Timer(internalCycle);
        cycleTimer.Elapsed += new System.Timers.ElapsedEventHandler((object? sender, System.Timers.ElapsedEventArgs events) =>
        {
            // 定时循环处理过期链接
            try
            {
                CycleProcess();
            }
            catch (Exception t)
            {   // 防御性容错
                Logger().LogError(t, "Unexpected error occur at collect statistic");
            }
        });
        cycleTimer.AutoReset = true;
        cycleTimer.Enabled = true;

        // 定时或者在接收到推送的消息后  主动-维护Pools集合
        var internalRecovery = this.Address.GetParameter(RECOVERY_PERIOD_KEY, DEFAULT_RECOVERY_PERIOD);

        recoveryTimer = new System.Timers.Timer(internalRecovery);
        recoveryTimer.Elapsed += new System.Timers.ElapsedEventHandler(async (object? sender, System.Timers.ElapsedEventArgs events) =>
        {
            // 定时循环恢复隔离区到正常区
            try
            {
                await RecoveryProcess();
            }
            catch (Exception t)
            {   // 防御性容错
                Logger().LogError(t, "Unexpected error occur at collect statistic");
            }
        });
        recoveryTimer.AutoReset = true;
        recoveryTimer.Enabled = true;
    }
    /// <summary>
    /// 定时循环处理过期链接
    /// </summary>
    void CycleProcess()
    {
        var overtime = this.Address.GetParameter(OVER_TIME_KEY, DEFAULT_OVER_TIME);
        var overtimeDate = DateTime.Now.AddMinutes(-overtime);
        foreach (var pool in Pools.Values)
        {
            pool.TimeOver(overtimeDate);
        }
    }

    /// <summary>
    /// 定时循环恢复隔离区到正常区
    /// </summary>
    async Task RecoveryProcess()
    {
        var recoverytime = this.Address.GetParameter(RECOVERY_TIME_KEY, DEFAULT_RECOVERY_TIME);
        var recoverytimeDate = DateTime.Now.AddMinutes(-recoverytime);

        try
        {
            await _semaphore.WaitAsync(100);
            foreach (var badUrls in this.BadUrls)
            {
                var list = new List<BadUrl>();
                foreach (var badUrl in badUrls.Value)
                {
                    if (badUrl.BadTime < recoverytimeDate)
                    {
                        this.Urls[badUrls.Key].Add(badUrl.Url);
                        list.Add(badUrl);
                        Console.WriteLine($"auto timer recovery url {badUrl.Url}");
                        Logger().LogInformation($"recovery:{badUrl.Url.ToString()}");
                    }
                }
                foreach (var item in list)
                {
                    badUrls.Value.Remove(item);
                }
            }
        }
        catch (Exception ex)
        {
            Logger().LogError(ex, ex.Message);
        }
        finally 
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 获取客户端服务连接
    /// </summary>
    /// <param name="invocation">服务路径</param>
    /// <returns>客户端服务连接</returns>
    private IClientPool GetClientPool(IInvocation invocation)
    {
        var invocationTypeName = invocation.TargetType.FullName!;
        //参数检查
        if (!Pools.ContainsKey(invocationTypeName))
        {
            throw new Exception($"not find the {invocation.TargetType.FullName}'s pool,please config it ");
        }

        var clientPool = Pools[invocationTypeName];
        clientPool.Address = Address;
        return clientPool;
    }
    /// <summary>
    /// 获取客户端缓存
    /// </summary>
    /// <param name="invocation">服务路径</param>
    /// <returns>客户端服务连接</returns>
    private ICache? GetCache(IInvocation invocation)
    {
        //参数检查
        if (Caches == null)
        {
            return null;
        }
        var invocationTypeName = invocation.TargetType.FullName!;
        var invocationMethodName = invocation.MethodInfo.Name;
        ICache? result = null;
        //app interface version
        var methodParameter = $"{invocation.AppPoint()}{invocationTypeName}.{invocationMethodName}{invocation.PointVersion()}";
        var key = this.Address.GetMethodParameter(methodParameter, CACHE_KEY, "");
        //app interface
        if (string.IsNullOrWhiteSpace(key))
        {
            key = this.Address.GetMethodParameter($"{invocation.AppPoint()}{invocationTypeName}.{invocationMethodName}", CACHE_KEY, "");
        }
        //interface version
        if (string.IsNullOrWhiteSpace(key))
        {
            key = this.Address.GetMethodParameter($"{invocationTypeName}.{invocationMethodName}{invocation.PointVersion()}", CACHE_KEY, "");
        }
        //interface
        if (string.IsNullOrWhiteSpace(key))
        {
            key = this.Address.GetMethodParameter($"{invocationTypeName}.{invocationMethodName}", CACHE_KEY, "");
        }
        //key
        if (string.IsNullOrWhiteSpace(key))
        {
            key = this.Address.GetParameter(CACHE_KEY, "");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return result;
        }

        if (key.ToLower() == "true" || key.ToLower() == LruCache.NAME)
        {
            result = Caches[LruCache.NAME];
        }

        if (Caches.ContainsKey(key) && key != LruCache.NAME)
        {
            result = Caches[key];
        }

        return result;
    }
    /// <summary>
    /// 选择负载均衡算法
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    private ILoadBalance GetLoadBalance(IInvocation invocation)
    {
        var result = LoadBalances[RandomLoadBalance.NAME];
        var invocationTypeName = invocation.TargetType.FullName!;
        var invocationMethodName = invocation.MethodInfo.Name;

        var key = this.Address.GetMethodParameter($"{invocationTypeName}.{ invocationMethodName }", LOADBANCE_KEY, "");

        if (string.IsNullOrEmpty(key))
        {
            key = this.Address.GetInterfaceParameter(invocationTypeName, LOADBANCE_KEY, "");
        }

        if (string.IsNullOrEmpty(key))
        {
            key = this.Address.GetParameter(LOADBANCE_KEY, "");
        }

        if (!string.IsNullOrEmpty(key) && LoadBalances.ContainsKey(key) && key != RandomLoadBalance.NAME)
        {
            result = LoadBalances[key];
        }
        return result;
    }

    /// <summary>
    /// 获取集群执行器
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    private ICluster GetCluster(IInvocation invocation)
    {
        var result = Clusters[FailoverCluster.NAME];
        var invocationTypeName = invocation.TargetType.FullName!;
        var invocationMethodName = invocation.MethodInfo.Name;

        var key = this.Address.GetMethodParameter($"{invocationTypeName}.{ invocationMethodName }", CLUSTER_KEY, "");

        if (string.IsNullOrEmpty(key))
        {
            key = this.Address.GetInterfaceParameter(invocationTypeName, CLUSTER_KEY, "");
        }
        if (string.IsNullOrEmpty(key))
        {
            key = this.Address.GetParameter(CLUSTER_KEY, "");
        }
        if (!string.IsNullOrEmpty(key) && Clusters.ContainsKey(key) && key != FailoverCluster.NAME)
        {
            result = Clusters[key];
        }
        
        return result;
    }
    /// <summary>
    /// 根据配置获取路径集合
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    private IList<URL> GetUrls(IInvocation invocation)
    {
        var invocationName = invocation.TargetType.FullName!;
        //参数检查
        if (!Urls.ContainsKey(invocationName) && !BadUrls.ContainsKey(invocationName))
        {
            throw new Exception($"not find the {invocation.TargetType.FullName}'s urls,please config it ");
        }

        if (Urls.TryGetValue(invocationName, out List<URL>? urls) && urls.Count>0)
        {
            var result = FilterUrls(invocation, urls);
            if (result?.Count > 0)
            {
                Logger().LogInformation("from good urls");
                return result;
            }
        }

        if (BadUrls.TryGetValue(invocationName, out List<BadUrl>? badUrls) && badUrls.Count>0)
        {
            var result = FilterUrls(invocation, badUrls.Select(w => w.Url).ToList());
            if (result?.Count > 0)
            {
                Logger().LogInformation("from bad urls");
                return result;
            }
        }

        throw new Exception($"not find the {invocation.AppPoint()}{invocationName}{invocation.PointVersion()}'s urls,please config it,config version must <= Url version ");
    }

    /// <summary>
    /// 将url集合中没有设置App或者和调用App一致的URL集合取出
    /// </summary>
    /// <param name="invocation"></param>
    /// <param name="urls"></param>
    /// <returns></returns>
    private IList<URL> FilterUrls(IInvocation invocation, IList<URL> urls)
    {
        var result = new List<URL>();
        var appResult = FilterAppUrls(invocation, urls);
        var versionResult = FilterVersionUrls(invocation, urls);
        result.AddRange(appResult);
        result.AddRange(versionResult);
        return result;

        /// <summary>
        /// 将url集合中没有设置App或者和调用App一致的URL集合取出
        /// </summary>
        /// <param name="invocation"></param>
        /// <param name="urls"></param>
        /// <returns></returns>
        IList<URL> FilterAppUrls(IInvocation invocation, IList<URL> urls)
        {
            if (string.IsNullOrEmpty(invocation.App)
                || urls.Count == 0)
            {
                return urls;
            }

            var result = new List<URL>();
            foreach (var item in urls)
            {
                var appName = item.GetParameter(APP_KEY);
                if (string.IsNullOrWhiteSpace(appName) || appName == invocation.App)
                {
                    result.Add(item);
                }
            }
            return result;
        }
        /// <summary>
        /// 将url集合中没有设置Version或者和调用Version一致或高于Version的URL集合取出
        /// </summary>
        /// <param name="invocation"></param>
        /// <param name="urls"></param>
        /// <returns></returns>
        IList<URL> FilterVersionUrls(IInvocation invocation, IList<URL> urls)
        {
            if (string.IsNullOrEmpty(invocation.Version)|| urls.Count == 0)
            {
                return urls;
            }

            var result = new List<URL>();
            foreach (var item in urls)
            {
                var version = item.GetParameter(URL.VERSION_KEY);
                if (string.IsNullOrWhiteSpace(version) || string.Compare(version, invocation.Version, true) >= 0)
                {
                    result.Add(item);
                }
            }
            return result;
        }
    }
    
    /// <summary>
    /// exec rpc
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    public async Task<IResult<T>> Invoke<T>(IInvocation invocation)
    {
        //cache
        var cache = this.GetCache(invocation);
        var invocationTypeName = invocation.TargetType.FullName!;
        var invocationMethodName = invocation.MethodInfo.Name;
        var parameters = invocation.MethodInfo.GetParameters();
        var key =$"{invocation.AppPoint()}{invocationTypeName}.{invocationMethodName}@{StringUtils.Md5(StringUtils.ToArgumentString(parameters, invocation.Arguments))}{invocation.PointVersion()}";
        if (cache != null)
        {
            var value = cache.Get<T>(key);
            if (value != null)
            {
                Logger().LogInformation($"call from cache:{key}");
                Logger().LogInformation($"cache type:{cache.GetType().FullName}");
                return new RpcResult<T>(value,0);
            }

            var resultInner = await this.InvokeInner<T>(invocation);
            if (!resultInner.HasException && resultInner.Value!=null)
            {
                cache.Put(key, resultInner.Value);
            }
            return resultInner;
        }

        var result = await this.InvokeInner<T>(invocation);
        return result;
    }

    /// <summary>
    /// clear all local cache 
    /// </summary>
    public void CacheClear()
    {
        if (Caches == null || Caches.Count <=0)
        {
            return;
        }

        foreach (var item in Caches)
        {
            item.Value.Clear();
        }
    }


    /// <summary>
    /// inner execute rpc calling
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    private async Task<IResult<T>> InvokeInner<T>(IInvocation invocation)
    {
        var urls = this.GetUrls(invocation);

        //get the url address
        if (urls == null || urls.Count == 0)
        {
            throw new Exception($"there is no alive url to access the remote interface {invocation.TargetType.FullName}");
        }

        //get load balance
        var loadbalance = this.GetLoadBalance(invocation);

        var cluster = this.GetCluster(invocation);

        var pool = this.GetClientPool(invocation);

        var result = await cluster.DoInvoke<T>(pool, loadbalance, this.Address, urls, invocation);

        try
        {
            await _semaphore.WaitAsync(100);

            var goodUrls = new List<URL>();
            // insulate the exception rpc url address 
            var badUrls = new List<BadUrl>();

            if (this.Urls.ContainsKey(invocation.TargetType.FullName!))
            {
                goodUrls = this.Urls[invocation.TargetType.FullName!];
            }

            if (this.BadUrls.ContainsKey(invocation.TargetType.FullName!))
            {
                badUrls = this.BadUrls[invocation.TargetType.FullName!];
            }

            //get all bad url, insulate from good url
            foreach (var item in result.BadUrls)
            {
                var goodUrl = goodUrls.FirstOrDefault(w => w == item.Url);
                //remove from good urls ,add to bad urls
                if (goodUrl == null)
                {
                    continue;
                }

                goodUrls.Remove(goodUrl);

                //refresh badurl timer
                var badUrl = badUrls.FirstOrDefault(w => w.Url == goodUrl);
                if (badUrl != null)
                {
                    badUrls.Remove(badUrl);
                }

                Logger().LogInformation(item.CurrentException, $"isolation url {item}");
                badUrls.Add(item);
            }

            //扫描所有正常调用的地址，将隔离区的自动恢复到正常区域
            foreach (var item in result.Urls)
            {
                var badUrl = badUrls.FirstOrDefault(w => w.Url == item);
                //从隔离区删除,添加到正常区域
                if (badUrl == null)
                {
                    continue;
                }

                badUrls.Remove(badUrl);

                if (!goodUrls.Contains(badUrl.Url))
                {
                    goodUrls.Add(badUrl.Url);
                    Logger().LogInformation($"recovery url {badUrl}");
                }
            }

            if (!this.BadUrls.ContainsKey(invocation.TargetType.FullName!) 
                && badUrls.Count() > 0)
            {
                this.BadUrls.TryAdd(invocation.TargetType.FullName!, badUrls);
            }
        }
        catch (Exception ex)
        {
            Logger().LogError(ex, ex.Message);
            throw;
        }
        finally {
            _semaphore.Release();
        }
        
        //是否有异常，并抛出异常
        if (result.IsThrow && result.ClusterException!=null)
        {
            throw result.ClusterException;
        }

        return result.Result;
    }

}


