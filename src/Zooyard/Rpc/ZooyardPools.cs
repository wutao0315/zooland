using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using Zooyard.DataAnnotations;
using Zooyard.Logging;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;
using Zooyard.Rpc.Route.None;
using Zooyard.Rpc.Route.State;
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
    public const string ROUTE_KEY = "route";
    public const string CYCLE_PERIOD_KEY = "cycle";
    public const int DEFAULT_CYCLE_PERIOD = 60 * 1000;
    public const string OVER_TIME_KEY = "overtime";
    public const int DEFAULT_OVER_TIME = 5;
    public const string RECOVERY_PERIOD_KEY = "recovery";
    public const int DEFAULT_RECOVERY_PERIOD = 6 * 1000;
    public const string RECOVERY_TIME_KEY = "recoverytime";
    public const int DEFAULT_RECOVERY_TIME = 5;
    public const string SERVIC_ENAME_KEY = "sn";

    /// <summary>
    /// 注册中心的配置
    /// </summary>
    private readonly IOptionsMonitor<ZooyardOption> _zooyard;
    ///// <summary>
    ///// good service url list
    ///// </summary>
    //public ConcurrentDictionary<string, List<URL>> Urls { get; init; }
    ///// <summary>
    ///// bad service url list
    ///// </summary>
    //public ConcurrentDictionary<string, List<BadUrl>> BadUrls { get; init; }
    /// <summary>
    /// the service pools
    /// key ApplicationName,
    /// value diff version of client pool
    /// </summary>
    public ConcurrentDictionary<string, IClientPool> Pools = new();
    /// <summary>
    /// loadbalance
    /// </summary>
    private readonly ConcurrentDictionary<string, ILoadBalance> _loadBalances = new();
    /// <summary>
    /// cluster
    /// </summary>
    private readonly ConcurrentDictionary<string, ICluster> _clusters = new();
    /// <summary>
    /// cache
    /// </summary>
    private readonly ConcurrentDictionary<string, ICache> _caches = new();
    
    /// <summary>
    /// 计时器用于处理过期的链接和链接池
    /// </summary>
    private System.Timers.Timer? cycleTimer;
    ///// <summary>
    ///// 计时器用于处理隔离区域自动恢复到正常区域
    ///// </summary>
    //private System.Timers.Timer? recoveryTimer;
    /// <summary>
    /// threed lock
    /// </summary>		
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, IStateRouterFactory> _routeFoctories = new();
    private readonly ConcurrentDictionary<string, (URL, IList<URL>)> _cacheUrl = new();
    private readonly ConcurrentDictionary<string, IList<URL>> _cacheRouteUrl = new();
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pool"></param>
    public ZooyardPools(
        IDictionary<string, IClientPool> pools,
        IEnumerable<ILoadBalance> loadbalances,
        IEnumerable<ICluster> clusters,
        IEnumerable<ICache> caches,
        IEnumerable<IStateRouterFactory> routerFactories,
        IOptionsMonitor<ZooyardOption> zooyard)
    {
        this.Pools = new(pools);

        foreach (var item in loadbalances)
        {
            _loadBalances[item.Name] = item;
        };

        foreach (var item in clusters)
        {
            _clusters[item.Name] = item;
        }

        foreach (var cache in caches)
        {
           _caches[cache.Name] = cache;
        }
        foreach (var item in routerFactories)
        {
            _routeFoctories[item.Name] = item;
        }
        //this.Urls = new ConcurrentDictionary<string, List<URL>>();
        //this.BadUrls = new ConcurrentDictionary<string, List<BadUrl>>();
        _zooyard = zooyard;
        _zooyard.OnChange(OnChanged);
        Init();
        // 初始化调用
        void Init()
        {
            // 定时或者在接收到推送的消息后  主动-维护Pools集合
            var internalCycle = _zooyard.CurrentValue.Meta.GetValue(CYCLE_PERIOD_KEY, DEFAULT_CYCLE_PERIOD);

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
            var internalRecovery = _zooyard.CurrentValue.Meta.GetValue(RECOVERY_PERIOD_KEY, DEFAULT_RECOVERY_PERIOD);

            //recoveryTimer = new System.Timers.Timer(internalRecovery);
            //recoveryTimer.Elapsed += new System.Timers.ElapsedEventHandler(async (object? sender, System.Timers.ElapsedEventArgs events) =>
            //{
            //    // 定时循环恢复隔离区到正常区
            //    try
            //    {
            //        await RecoveryProcess();
            //    }
            //    catch (Exception t)
            //    {   // 防御性容错
            //        Logger().LogError(t, "Unexpected error occur at collect statistic");
            //    }
            //});
            //recoveryTimer.AutoReset = true;
            //recoveryTimer.Enabled = true;

            // 定时循环处理过期链接
            void CycleProcess()
            {
                var overtime = _zooyard.CurrentValue.Meta.GetValue(OVER_TIME_KEY, DEFAULT_OVER_TIME);
                var overtimeDate = DateTime.Now.AddMinutes(-overtime);
                foreach (var pool in Pools)
                {
                    pool.Value.TimeOver(overtimeDate);
                }
            }

            //// 定时循环恢复隔离区到正常区
            //async Task RecoveryProcess()
            //{
            //    var recoverytime = _zooyard.CurrentValue.Meta.GetValue(RECOVERY_TIME_KEY, DEFAULT_RECOVERY_TIME);
            //    var recoverytimeDate = DateTime.Now.AddMinutes(-recoverytime);

            //    try
            //    {
            //        await _semaphore.WaitAsync();
            //        foreach (var badUrls in this.BadUrls)
            //        {
            //            var list = new List<BadUrl>();
            //            foreach (var badUrl in badUrls.Value)
            //            {
            //                if (badUrl.BadTime < recoverytimeDate)
            //                {
            //                    this.Urls[badUrls.Key].Add(badUrl.Url);
            //                    list.Add(badUrl);
            //                    Console.WriteLine($"auto timer recovery url {badUrl.Url}");
            //                    Logger().LogInformation($"recovery:{badUrl.Url.ToString()}");
            //                }
            //            }
            //            foreach (var item in list)
            //            {
            //                badUrls.Value.Remove(item);
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger().LogError(ex, ex.Message);
            //    }
            //    finally
            //    {
            //        _semaphore.Release();
            //    }
            //}
        }
        // 监听配置或者服务注册变化，清空缓存
        void OnChanged(ZooyardOption value, string name)
        {
            _cacheUrl.Clear();
            _cacheRouteUrl.Clear();
            foreach (var item in _routeFoctories)
            {
                item.Value.ClearCache();
            }
        }
    }

    /// <summary>
    /// exec rpc
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    public async Task<IResult<T>?> Invoke<T>(IInvocation invocation)
    {
        RpcContext.GetContext().SetInvocation(invocation);

        var (address, urls) = GetUrls();

        //cache
        var cache = GetCache();

        if (cache == null)
        {
            var result = await InvokeInner();
            return result;
        }

        var invocationTypeName = invocation.TargetType.FullName!;
        var invocationMethodName = invocation.MethodInfo.Name;
        var parameters = invocation.MethodInfo.GetParameters();
        var key = $"{invocation.ServiceNamePoint()}{invocationTypeName}.{invocationMethodName}@{StringUtils.Md5(StringUtils.ToArgumentString(parameters, invocation.Arguments))}{invocation.PointVersion()}";
        var value = cache.Get<T>(key);
        if (value != null)
        {
            Logger().LogInformation($"call from cache({cache.GetType().FullName}):{key}");
            return new RpcResult<T>(value, 0);
        }

        var resultInner = await InvokeInner();

        if (resultInner!=null 
            && !resultInner.HasException 
            && resultInner.Value != null)
        {
            cache.Put(key, resultInner.Value);
        }

        return resultInner;

        // 根据配置获取路径集合
        (URL, IList<URL>) GetUrls()
        {
            //读取缓存
            var cacheKey = $"{invocation.ServiceNamePoint()}{invocation.PointVersion()}";

            if (_cacheUrl.TryGetValue(cacheKey, out (URL, IList<URL>) val)) 
            {
                return val;
            }

            var url = string.IsNullOrWhiteSpace(_zooyard.CurrentValue.Address) ? URL.ValueOf("zooyard://localhost") : URL.ValueOf(_zooyard.CurrentValue.Address);

            url = GetMetaUrl(url, _zooyard.CurrentValue.Meta);

            url = GetUrl(url, invocation.Url);

            var result = new List<URL>();
            if (_zooyard.CurrentValue.Services.TryGetValue(invocation.ServiceName, out ZooyardServiceOption? service))
            {
                url = GetMetaUrl(url, service.Meta);

                foreach (var item in service.Instances)
                {
                    var itemUrl = url.SetHost(item.Host);
                    itemUrl = itemUrl.SetPort(item.Port);
                    itemUrl = GetMetaUrl(itemUrl, item.Meta);
                    result.Add(itemUrl);
                }
            }

            if (result.Count == 0)
            {
                result.Add(url);
            }

            _cacheUrl[cacheKey] = (url, result);
            return _cacheUrl[cacheKey];

            URL GetMetaUrl(URL url, Dictionary<string, string> meta)
            {
                foreach (var item in meta)
                {
                    switch (item.Key)
                    {
                        case string a when a.Equals("protocol", StringComparison.OrdinalIgnoreCase):
                            url = url.SetProtocol(item.Value);
                            break;
                        case string a when a.Equals("username", StringComparison.OrdinalIgnoreCase):
                            url = url.SetUsername(item.Value);
                            break;
                        case string a when a.Equals("password", StringComparison.OrdinalIgnoreCase):
                            url = url.SetPassword(item.Value);
                            break;
                        case string a when a.Equals("host", StringComparison.OrdinalIgnoreCase):
                            url = url.SetHost(item.Value);
                            break;
                        case string a when a.Equals("port", StringComparison.OrdinalIgnoreCase):
                            if (int.TryParse(item.Value, out int port))
                            {
                                url = url.SetPort(port);
                            }
                            break;
                        case string a when a.Equals("path", StringComparison.OrdinalIgnoreCase):
                            url = url.SetPath(item.Value);
                            break;
                        default:
                            url = url.AddParameter(item.Key, item.Value);
                            break;
                    }
                }
                return url;
            }
            URL GetUrl(URL url, string urlString)
            {
                if (string.IsNullOrWhiteSpace(urlString))
                {
                    return url;
                }
                var u = URL.ValueOf(urlString);
                url = url.SetProtocol(u.Protocol);
                url = url.SetUsername(u.Username);
                url = url.SetPassword(u.Password);
                url = url.SetHost(u.Host);
                url = url.SetPort(u.Port);
                url = url.SetPath(u.Path);
                foreach (var item in u.Parameters)
                {
                    url = url.AddParameter(item.Key, item.Value);
                }
                return url;
            }
        }

        // 获取客户端缓存
        ICache? GetCache()
        {
            //参数检查
            if (_caches == null || _caches.Count == 0)
            {
                return null;
            }

            var invocationTypeName = invocation.TargetType.FullName!;
            var invocationMethodName = invocation.MethodInfo.Name;
            ICache? result = null;
            var key = address.GetParameter($"{invocationTypeName}.{invocationMethodName}{invocation.PointVersion()}.{CACHE_KEY}", "");
            //interface
            if (string.IsNullOrWhiteSpace(key))
            {
                key = address.GetParameter($"{invocationTypeName}.{invocationMethodName}.{CACHE_KEY}", "");
            }
            //key
            if (string.IsNullOrWhiteSpace(key))
            {
                key = address.GetParameter(CACHE_KEY, "");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return result;
            }

            if (bool.TrueString.Equals(key, StringComparison.OrdinalIgnoreCase) 
                || LruCache.NAME.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                result = _caches[LruCache.NAME];
            }

            if (_caches.ContainsKey(key) && key != LruCache.NAME)
            {
                result = _caches[key];
            }

            return result;
        }

        IList<URL> GetRouteUrls()
        {
            var cacheKey = $"{invocation.ServiceNamePoint()}{invocation.PointVersion()}";

            if (_cacheRouteUrl.TryGetValue(cacheKey, out IList<URL>? val) && val != null)
            {
                return val;
            }

            var routerFactory = _routeFoctories[NoneStateRouterFactory.NAME];
            var invocationTypeName = invocation.TargetType.FullName!;
            var invocationMethodName = invocation.MethodInfo.Name;

            var key = address.GetMethodParameter($"{invocationTypeName}.{invocationMethodName}", ROUTE_KEY, "");

            if (string.IsNullOrWhiteSpace(key))
            {
                key = address.GetInterfaceParameter(invocationTypeName, ROUTE_KEY, "");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                key = address.GetParameter(ROUTE_KEY, "");
            }

            if (!string.IsNullOrWhiteSpace(key) && _loadBalances.ContainsKey(key) && key != NoneStateRouterFactory.NAME)
            {
                routerFactory = _routeFoctories[key];
            }

            //执行过滤逻辑
            var stateRoute = routerFactory.GetRouter(invocation.TargetType, address);

            var needToPrintMessageStr = address.GetParameter($"{invocation.ServiceName}.{invocation.MethodInfo.Name}.needToPrintMessage", "");

            if (string.IsNullOrWhiteSpace(needToPrintMessageStr))
            {
                needToPrintMessageStr = address.GetParameter($"{invocation.MethodInfo.Name}.needToPrintMessage", "");
            }
            if (string.IsNullOrWhiteSpace(needToPrintMessageStr))
            {
                needToPrintMessageStr = address.GetParameter("needToPrintMessage", "");
            }

            _ = bool.TryParse(needToPrintMessageStr, out bool needToPrintMessage);

            var result = stateRoute.Route(urls, address, invocation, needToPrintMessage);

            _cacheRouteUrl[cacheKey] = result;
            return result;
        }
        // 执行调用
        async Task<IResult<T>?> InvokeInner() 
        {
            //get cached route urls
            var routeUrls = GetRouteUrls();

            RpcContext.GetContext().SetInvokers(routeUrls);

            var header = new Dictionary<string, string>();
            var targetDescription = invocation.TargetType.GetCustomAttribute<RequestMappingAttribute>();
            if (targetDescription != null)
            {
                foreach (var item in targetDescription.Headers)
                {
                    RpcContext.GetContext().SetAttachment(item.Key, item.Value);
                }
            }
            var methodDescription = invocation.MethodInfo.GetCustomAttribute<RequestMappingAttribute>();
            if (methodDescription != null)
            {
                foreach (var item in methodDescription.Headers)
                {
                    RpcContext.GetContext().SetAttachment(item.Key, item.Value);
                }
            }


            //get pool
            var pool = GetClientPool(invocation);
            //get load balance
            var loadbalance = GetLoadBalance(address, invocation);
            //get cluster
            var cluster = GetCluster(address, invocation);
            //invoke
            var result = await cluster.Invoke<T>(pool, loadbalance, address, routeUrls, invocation);

            //try
            //{
            //    await _semaphore.WaitAsync();

            //    var goodUrls = new List<URL>();
            //    // insulate the exception rpc url address 
            //    var badUrls = new List<BadUrl>();

            //    if (this.Urls.ContainsKey(invocation.TargetType.FullName!))
            //    {
            //        goodUrls = this.Urls[invocation.TargetType.FullName!];
            //    }

            //    if (this.BadUrls.ContainsKey(invocation.TargetType.FullName!))
            //    {
            //        badUrls = this.BadUrls[invocation.TargetType.FullName!];
            //    }

            //    //get all bad url, insulate from good url
            //    foreach (var item in result.BadUrls)
            //    {
            //        var goodUrl = goodUrls.FirstOrDefault(w => w == item.Url);
            //        //remove from good urls ,add to bad urls
            //        if (goodUrl == null)
            //        {
            //            continue;
            //        }

            //        goodUrls.Remove(goodUrl);

            //        //refresh badurl timer
            //        var badUrl = badUrls.FirstOrDefault(w => w.Url == goodUrl);
            //        if (badUrl != null)
            //        {
            //            badUrls.Remove(badUrl);
            //        }

            //        Logger().LogInformation(item.CurrentException, $"isolation url {item}");
            //        badUrls.Add(item);
            //    }

            //    //扫描所有正常调用的地址，将隔离区的自动恢复到正常区域
            //    foreach (var item in result.Urls)
            //    {
            //        var badUrl = badUrls.FirstOrDefault(w => w.Url == item);
            //        //从隔离区删除,添加到正常区域
            //        if (badUrl == null)
            //        {
            //            continue;
            //        }

            //        badUrls.Remove(badUrl);

            //        if (!goodUrls.Contains(badUrl.Url))
            //        {
            //            goodUrls.Add(badUrl.Url);
            //            Logger().LogInformation($"recovery url {badUrl}");
            //        }
            //    }

            //    if (!this.BadUrls.ContainsKey(invocation.TargetType.FullName!)
            //        && badUrls.Count() > 0)
            //    {
            //        this.BadUrls.TryAdd(invocation.TargetType.FullName!, badUrls);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logger().LogError(ex, ex.Message);
            //    throw;
            //}
            //finally
            //{
            //    _semaphore.Release();
            //}

            //是否有异常，并抛出异常
            if (result.IsThrow && result.ClusterException != null)
            {
                throw result.ClusterException;
            }

            return result.Result;
        }

       

        // 获取客户端服务连接
        IClientPool GetClientPool(IInvocation invocation)
        {
            var invocationTypeName = invocation.TargetType.FullName!;
            //参数检查
            if (!Pools.ContainsKey(invocationTypeName))
            {
                throw new Exception($"not find the {invocation.TargetType.FullName}'s pool,please config it ");
            }

            var clientPool = Pools[invocationTypeName];
            return clientPool;
        }

        // 选择负载均衡算法
        ILoadBalance GetLoadBalance(URL address, IInvocation invocation)
        {
            var result = _loadBalances[RandomLoadBalance.NAME];
            var invocationTypeName = invocation.TargetType.FullName!;
            var invocationMethodName = invocation.MethodInfo.Name;

            var key = address.GetMethodParameter($"{invocationTypeName}.{invocationMethodName}", LOADBANCE_KEY, "");

            if (string.IsNullOrWhiteSpace(key))
            {
                key = address.GetInterfaceParameter(invocationTypeName, LOADBANCE_KEY, "");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                key = address.GetParameter(LOADBANCE_KEY, "");
            }

            if (!string.IsNullOrWhiteSpace(key) && _loadBalances.ContainsKey(key) && key != RandomLoadBalance.NAME)
            {
                result = _loadBalances[key];
            }
            return result;
        }

        // 获取集群执行器
        ICluster GetCluster(URL address, IInvocation invocation)
        {
            var result = _clusters[FailoverCluster.NAME];
            var invocationTypeName = invocation.TargetType.FullName!;
            var invocationMethodName = invocation.MethodInfo.Name;

            var key = address.GetMethodParameter($"{invocationTypeName}.{invocationMethodName}", CLUSTER_KEY, "");

            if (string.IsNullOrWhiteSpace(key))
            {
                key = address.GetInterfaceParameter(invocationTypeName, CLUSTER_KEY, "");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                key = address.GetParameter(CLUSTER_KEY, "");
            }
            if (!string.IsNullOrWhiteSpace(key) && _clusters.ContainsKey(key) && key != FailoverCluster.NAME)
            {
                result = _clusters[key];
            }

            return result;
        }
    }

    /// <summary>
    /// clear all local cache 
    /// </summary>
    public void CacheClear()
    {
        if (_caches == null || _caches.IsEmpty)
        {
            return;
        }

        foreach (var item in _caches)
        {
            item.Value.Clear();
        }
    }
}

