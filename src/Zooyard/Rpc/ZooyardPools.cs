using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using Zooyard.Attributes;
using Zooyard.Management;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;
using Zooyard.Rpc.Route;
using Zooyard.Rpc.Route.None;
using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc;

/// <summary>
/// Singleton object manage pools
/// </summary>
public class ZooyardPools : IZooyardPools
{
    private readonly ILogger _logger;

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

    ///// <summary>
    ///// 注册中心的配置
    ///// </summary>
    //private readonly IOptionsMonitor<FeignOption> _zooyard;
    private readonly IRpcStateLookup _proxyStateLookup;
    private readonly IDisposable _configChange;
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
    private readonly PeriodicTimer _cycleTimer;

    /// <summary>
    /// 计时器用于处理隔离区域自动恢复到正常区域
    /// </summary>
    private readonly PeriodicTimer _recoveryTimer;
    private readonly ConcurrentDictionary<string, IStateRouterFactory> _routeFoctories = new();
    private readonly ConcurrentDictionary<string, (URL, IList<URL>)> _cacheUrl = new();
    private readonly ConcurrentDictionary<string, IList<URL>> _cacheRouteUrl = new();
    private readonly ConcurrentDictionary<string, List<BadUrl>> _badUrls = new();


    public ZooyardPools(ILoggerFactory loggerFactory
        , IHostApplicationLifetime appLifetime
        , IDictionary<string, IClientPool> pools
        , IEnumerable<ILoadBalance> loadbalances
        , IEnumerable<ICluster> clusters
        , IEnumerable<ICache> caches
        , IEnumerable<IStateRouterFactory> routerFactories
        , IRpcStateLookup proxyStateLookup
        )
    {
        _logger = loggerFactory.CreateLogger<ZooyardPools>();
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

        _proxyStateLookup = proxyStateLookup;
        _configChange = _proxyStateLookup.OnChange(OnChanged);

        // 定时或者在接收到推送的消息后  主动-维护Pools集合
        var internalCycle = _proxyStateLookup.GetGlobalMataValue(CYCLE_PERIOD_KEY, DEFAULT_CYCLE_PERIOD);
        _cycleTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(internalCycle));

        // 定时或者在接收到推送的消息后  主动-维护Pools集合
        var internalRecovery = _proxyStateLookup.GetGlobalMataValue(RECOVERY_PERIOD_KEY, DEFAULT_RECOVERY_PERIOD);
        _recoveryTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(internalRecovery));

        // 监听配置或者服务注册变化，清空缓存
        void OnChanged(IRpcStateLookup state)
        {
            try
            {
                _cacheUrl.Clear();
                _cacheRouteUrl.Clear();
                foreach (var item in _routeFoctories)
                {
                    item.Value.ClearCache();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        // Register these last as the callbacks could run immediately
        appLifetime.ApplicationStarted.Register(Start);
        appLifetime.ApplicationStopping.Register(Close);
    }

    public void Start()
    {
        // Start the timer loop
        _ = CycleTimerLoop();
        _ = RecoveryTimerLoop();
    }

    private async Task CycleTimerLoop()
    {
        using (_cycleTimer)
        {
            // The TimerAwaitable will return true until Stop is called
            while (await _cycleTimer.WaitForNextTickAsync())
            {
                try
                {
                    CycleProcess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occur at collect statistic");
                }
            }
        }

        // 定时循环处理过期链接
        void CycleProcess()
        {
            var overtime = _proxyStateLookup.GetGlobalMataValue(OVER_TIME_KEY, DEFAULT_OVER_TIME);
            var overtimeDate = DateTime.Now.AddMinutes(-overtime);
            foreach (var pool in Pools)
            {
                pool.Value.TimeOver(overtimeDate);
            }
        }
    }

    private async Task RecoveryTimerLoop()
    {
        using (_recoveryTimer)
        {
            // The TimerAwaitable will return true until Stop is called
            while (await _recoveryTimer.WaitForNextTickAsync())
            {
                try
                {
                    RecoveryProcess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occur at collect statistic");
                }
            }
        }

        // 定时循环恢复隔离区到正常区
        void RecoveryProcess()
        {
            var recoverytime = _proxyStateLookup.GetGlobalMataValue(RECOVERY_TIME_KEY, DEFAULT_RECOVERY_TIME);
            var recoverytimeDate = DateTime.Now.AddMinutes(-recoverytime);

            try
            {
                var keyList = new List<string>();
                foreach (var badUrls in _badUrls)
                {
                    var list = new List<BadUrl>();
                    foreach (var badUrl in badUrls.Value)
                    {
                        if (badUrl.BadTime < recoverytimeDate)
                        {
                            list.Add(badUrl);
                            Console.WriteLine($"auto timer recovery url {badUrl.Url}");
                            _logger.LogInformation($"recovery:{badUrl.Url.ToString()}");
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
                _logger.LogError(ex, ex.Message);
            }
        }
    }

    public void Close()
    {
        // Stop firing the timer
        _cycleTimer.Dispose();
        _recoveryTimer.Dispose();
        _configChange.Dispose();
    }
    /// <summary>
    /// exec rpc
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    public async Task<IResult<T>?> Invoke<T>(IInvocation invocation)
    {
        var (proxyAddr, urls) = GetUrls();

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
            _logger.LogInformation($"call from cache({cache.GetType().FullName}):{key}");
            return new RpcResult<T>(value);
        }

        var resultInner = await InvokeInner();

        if (resultInner != null
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
            var cacheKey = $"{invocation.ServiceName}{invocation.PointVersion()}";

            if (_cacheUrl.TryGetValue(cacheKey, out (URL, IList<URL>) val))
            {
                return val;
            }

            var url = invocation.Url;

            url = GetRouteMetaUrl(url, invocation.ServiceName);
            //url = GetUrl(url, invocation.Url);

            var result = new List<URL>();
            if (_proxyStateLookup.TryGetService(invocation.ServiceName, out var service))
            {
                url = GetMetaUrl(url, service.Model.Config.Metadata);

                if (service.Instances != null && service.Instances.Count > 0)
                {
                    foreach (var item in service.Instances)
                    {
                        var itemUrl = url.SetHost(item.Value.Model.Config.Host);
                        itemUrl = itemUrl.SetPort(item.Value.Model.Config.Port);
                        itemUrl = GetMetaUrl(itemUrl, item.Value.Model.Config.Metadata);
                        result.Add(itemUrl);
                    }
                }
            }

            if (result.Count == 0)
            {
                result.Add(url);
            }

            _cacheUrl[cacheKey] = (url, result);
            return _cacheUrl[cacheKey];
            URL GetRouteMetaUrl(URL url, string serviceId)
            {
                foreach (var item in _proxyStateLookup.GetRoutes().OrderBy(w => w.Config.Order))
                {
                    if (string.IsNullOrWhiteSpace(item.Config.ServicePattern)
                        || item.Config.Metadata == null
                        || item.Config.Metadata.Count == 0)
                    {
                        continue;
                    }
                    if (Regex.IsMatch(serviceId, item.Config.ServicePattern))
                    {
                        url = GetMetaUrl(url, item.Config.Metadata);
                    }
                }
                return url;
            }
            URL GetMetaUrl(URL url, IDictionary<string, string>? meta)
            {
                if (meta == null)
                {
                    return url;
                }
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
                        case string a when a.Equals(CommonConstants.BASE_KEY, StringComparison.OrdinalIgnoreCase):
                            var basePath = item.Value.EndsWith('/') ? item.Value[..^1] : item.Value;
                            var path = url.Path.StartsWith('/') ? url.Path : "/" + url.Path;
                            url = url.SetPath(basePath + path);
                            break;
                        case string a when a.Equals(CommonConstants.PATH_KEY, StringComparison.OrdinalIgnoreCase):
                            url = url.SetPath(item.Value);
                            break;
                        default:
                            url = url.AddParameter(item.Key, item.Value);
                            break;
                    }
                }
                return url;
            }
            //URL GetUrl(URL url, URL? u)
            //{
            //    if (u == null)
            //    {
            //        return url;
            //    }
            //    url = url.SetProtocol(u.Protocol);
            //    url = url.SetUsername(u.Username);
            //    url = url.SetPassword(u.Password);
            //    url = url.SetHost(u.Host);
            //    url = url.SetPort(u.Port);

            //    url = url.SetPath(u.Path);

            //    foreach (var item in u.Parameters)
            //    {
            //        url = url.AddParameter(item.Key, item.Value);
            //    }
            //    return url;
            //}
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
            var key = proxyAddr.GetParameter($"{invocationTypeName}.{invocationMethodName}{invocation.PointVersion()}.{CACHE_KEY}", "");
            //interface
            if (string.IsNullOrWhiteSpace(key))
            {
                key = proxyAddr.GetParameter($"{invocationTypeName}.{invocationMethodName}.{CACHE_KEY}", "");
            }
            //key
            if (string.IsNullOrWhiteSpace(key))
            {
                key = proxyAddr.GetParameter(CACHE_KEY, "");
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

        // 执行调用
        async Task<IResult<T>?> InvokeInner()
        {

            //get cached route urls
            var routeUrls = GetRouteUrls();
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
            var loadbalance = GetLoadBalance(proxyAddr, invocation);
            //get cluster
            var cluster = GetCluster(proxyAddr, invocation);
            //bad urls
            var badUrls = _badUrls.GetOrAdd(invocation.TargetType.FullName!, new List<BadUrl>());

            //invoke
            var result = await cluster.Invoke<T>(pool, loadbalance, proxyAddr, routeUrls, badUrls, invocation);
            try
            {
                //get all bad url, insulate from good url
                foreach (var item in result.BadUrls)
                {
                    //refresh badurl timer
                    var badUrl = badUrls.FirstOrDefault(w => w.Url == item.Url);
                    if (badUrl != null)
                    {
                        badUrl.BadTime = item.BadTime;
                        badUrl.CurrentException = item.CurrentException;
                        continue;
                    }

                    _logger.LogInformation(item.CurrentException, $"isolation url {item}");
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            //是否有异常，并抛出异常
            if (result.IsThrow && result.ClusterException != null)
            {
                throw result.ClusterException;
            }

            return result.Result;
        }

        //执行路由逻辑
        IList<URL> GetRouteUrls()
        {
            var cacheKey = $"{invocation.ServiceName}{invocation.PointVersion()}";

            if (_cacheRouteUrl.TryGetValue(cacheKey, out IList<URL>? val) && val != null)
            {
                return val;
            }

            var routerFactory = _routeFoctories[NoneStateRouterFactory.NAME];
            var invocationTypeName = invocation.TargetType.FullName!;
            var invocationMethodName = invocation.MethodInfo.Name;

            var key = proxyAddr.GetMethodParameter($"{invocationTypeName}.{invocationMethodName}", ROUTE_KEY, "");

            if (string.IsNullOrWhiteSpace(key))
            {
                key = proxyAddr.GetInterfaceParameter(invocationTypeName, ROUTE_KEY, "");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                key = proxyAddr.GetParameter(ROUTE_KEY, "");
            }

            if (!string.IsNullOrWhiteSpace(key)
                && _loadBalances.ContainsKey(key)
                && key != NoneStateRouterFactory.NAME
                && _routeFoctories.ContainsKey(key))
            {
                routerFactory = _routeFoctories[key];
            }

            //执行过滤逻辑
            var stateRoute = routerFactory.GetRouter(invocation.TargetType, proxyAddr);

            var needToPrintMessageStr = proxyAddr.GetParameter($"{invocation.ServiceName}.{invocation.MethodInfo.Name}.needToPrintMessage", "");

            if (string.IsNullOrWhiteSpace(needToPrintMessageStr))
            {
                needToPrintMessageStr = proxyAddr.GetParameter($"{invocation.MethodInfo.Name}.needToPrintMessage", "");
            }
            if (string.IsNullOrWhiteSpace(needToPrintMessageStr))
            {
                needToPrintMessageStr = proxyAddr.GetParameter("needToPrintMessage", "");
            }

            Holder<RouterSnapshotNode>? nodeHolder = null;
            if (bool.TryParse(needToPrintMessageStr, out bool needToPrintMessage) && needToPrintMessage)
            {
                nodeHolder = new Holder<RouterSnapshotNode>
                {
                    Value = new RouterSnapshotNode(invocation.ServiceNamePoint(), urls)
                };
            }

            var result = stateRoute.Route(urls, proxyAddr, invocation, needToPrintMessage, nodeHolder);

            _cacheRouteUrl[cacheKey] = result;
            return result;
        }

        // 获取客户端服务连接
        IClientPool GetClientPool(IInvocation invocation)
        {
            var invocationTypeName = invocation.TargetType.FullName!;
            //参数检查
            if (!Pools.ContainsKey(invocationTypeName))
            {
                throw new RpcException($"not find the {invocation.TargetType.FullName}'s pool,please config it ");
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

            if (!string.IsNullOrWhiteSpace(key)
                && _loadBalances.ContainsKey(key)
                && key != RandomLoadBalance.NAME)
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
            if (!string.IsNullOrWhiteSpace(key)
                && _clusters.ContainsKey(key)
                && key != FailoverCluster.NAME)
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
