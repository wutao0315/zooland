using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Zooyard.Core;
using Zooyard.Core.Utils;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;

namespace Zooyard.Rpc
{
    //单例对象
    public class ZooyardPools : IZooyardPools
    {
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
        //protected ILog logger = LogManager.GetLogger<ZooyardPools>();
        /// <summary>
        /// 地址
        /// 如果是registry开头，就代表是注册中心的地址
        /// 否则就是直连地址
        /// </summary>
        public URL Address { get; private set; }
        /// <summary>
        /// 链接地址集合
        /// </summary>
        public ConcurrentDictionary<string, IList<URL>> Urls { get; private set; }
        /// <summary>
        /// 失败链接地址集合
        /// </summary>
        public ConcurrentDictionary<string, IList<BadUrl>> BadUrls { get; private set; }
        /// <summary>
        /// 注册中心发现机制
        /// </summary>
        public IRegistryHost RegistryHost { get; set; }
        /// <summary>
        /// 服务集合
        /// key 是ApplicationName,
        /// value是不同版本的客户端链接池
        /// </summary>
        public ConcurrentDictionary<string, IClientPool> Pools { get; private set; }
        /// <summary>
        /// 负载均衡
        /// </summary>
        public ConcurrentDictionary<string, ILoadBalance> LoadBalances { get; private set; }
        /// <summary>
        /// 集群
        /// </summary>
        public ConcurrentDictionary<string, ICluster> Clusters { get; private set; }
        /// <summary>
        /// 缓存集合
        /// </summary>
        public ConcurrentDictionary<string, ICache> Caches { get; private set; }
        /// <summary>
        /// 计时器用于处理过期的链接和链接池
        /// </summary>
        private System.Timers.Timer cycleTimer;
        /// <summary>
        /// 计时器用于处理隔离区域自动恢复到正常区域
        /// </summary>
        private System.Timers.Timer recoveryTimer;
        /// <summary>
        /// 多线程锁
        /// </summary>		
        protected object locker = new object();
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pools"></param>
        public ZooyardPools(IDictionary<string, IClientPool> pools,
            IDictionary<string, ILoadBalance> loadbalances,
            IDictionary<string, ICluster> clusters,
            IDictionary<string, Type> caches,
            string address)
            : this(pools, loadbalances, clusters, caches, address, new List<string>()) { }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pools"></param>
        public ZooyardPools(IDictionary<string, IClientPool> pools,
            IDictionary<string, ILoadBalance> loadbalances,
            IDictionary<string, ICluster> clusters,
            IDictionary<string, Type> caches,
            string address,
            IList<string> urls)
        {
            this.Pools = new ConcurrentDictionary<string, IClientPool>(pools);
            this.LoadBalances = new ConcurrentDictionary<string, ILoadBalance>(loadbalances);
            this.Clusters = new ConcurrentDictionary<string, ICluster>(clusters);
            this.Address = URL.valueOf(address);
            this.Urls = new ConcurrentDictionary<string, IList<URL>>();
            this.BadUrls = new ConcurrentDictionary<string, IList<BadUrl>>();
            //参数
            var u = new List<URL>();
            foreach (var url in urls)
            {
                u.Add(URL.valueOf(url));
            }

            foreach (var item in u.GroupBy(w => w.ServiceInterface))
            {
                this.Urls.TryAdd(item.Key, item.ToList());
            }

            this.Caches = new ConcurrentDictionary<string, ICache>();
            foreach (var cache in caches)
            {
                var value = cache.Value.GetConstructor(new Type[] { typeof(URL) })
                    .Invoke(new object[] { this.Address }) as ICache;

                this.Caches.TryAdd(cache.Key, value);
            }

            Init();
        }

        /// <summary>
        /// 初始化调用
        /// </summary>
        public void Init()
        {
            //路径地址-用于初始化基础数据
            if (Address.Protocol.ToLower().StartsWith("registry"))// 用户指定URL，指定的URL 对点对直连地址
            {
                // 通过注册中心配置拼装URL
                var urls = this.RegistryHost.FindAll();
                foreach (var item in urls.GroupBy(w => w.ServiceInterface))
                {
                    if (this.Urls.ContainsKey(item.Key))
                    {
                        foreach (var i in item)
                        {
                            if (!this.Urls[item.Key].Contains(i))
                            {
                                this.Urls[item.Key].Add(i);
                            }
                        }
                    }
                    else
                    {
                        this.Urls.TryAdd(item.Key, item.ToList());
                    }
                }
            }

            // 定时或者在接收到推送的消息后  主动-维护Pools集合
            var internalCycle = this.Address.GetParameter(CYCLE_PERIOD_KEY, DEFAULT_CYCLE_PERIOD);

            cycleTimer = new System.Timers.Timer(internalCycle);
            cycleTimer.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs events) =>
            {
                // 收集统计信息
                try
                {
                    //Thread.Sleep(TimeSpan.FromMilliseconds(internalCycle));
                    cycleProcess();
                }
                catch (Exception t)
                {   // 防御性容错
                    //logger.Error("Unexpected error occur at collect statistic", t);
                }
            });
            cycleTimer.AutoReset = true;
            cycleTimer.Enabled = true;

            // 定时或者在接收到推送的消息后  主动-维护Pools集合
            var internalRecovery = this.Address.GetParameter(RECOVERY_PERIOD_KEY, DEFAULT_RECOVERY_PERIOD);

            recoveryTimer = new System.Timers.Timer(internalRecovery);
            recoveryTimer.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs events) =>
            {
                // 收集统计信息
                try
                {
                    //Thread.Sleep(TimeSpan.FromMilliseconds(internalCycle));
                    recoveryProcess();
                }
                catch (Exception t)
                {   // 防御性容错
                    //logger.Error("Unexpected error occur at collect statistic", t);
                }
            });
            recoveryTimer.AutoReset = true;
            recoveryTimer.Enabled = true;
        }
        /// <summary>
        /// 定时循环处理过期链接
        /// </summary>
        void cycleProcess()
        {
            var overtime = this.Address.GetParameter(OVER_TIME_KEY, DEFAULT_OVER_TIME);
            var overtimeDate = DateTime.Now.AddMinutes(-overtime);
            foreach (var pool in Pools.Values)
            {
                pool.TimeOver(overtimeDate);
            }
            //Console.WriteLine("cycle timer actived");
        }

        /// <summary>
        /// 定时循环恢复隔离区到正常区
        /// </summary>
        void recoveryProcess()
        {
            var recoverytime = this.Address.GetParameter(RECOVERY_TIME_KEY, DEFAULT_RECOVERY_TIME);
            var recoverytimeDate = DateTime.Now.AddMinutes(-recoverytime);

            lock (locker)
            {
                foreach (var badUrls in BadUrls)
                {
                    var list = new List<BadUrl>();
                    foreach (var badUrl in badUrls.Value)
                    {
                        if (badUrl.BadTime < recoverytimeDate)
                        {
                            this.Urls[badUrls.Key].Add(badUrl.Url);
                            list.Add(badUrl);
                            Console.WriteLine($"auto timer recovery url {badUrl.Url.ToString()}");
                            //logger.Debug($"recovery:{badUrl.Url.ToString()}");
                        }
                    }
                    //delete badurl from badurls 
                    foreach (var item in list)
                    {
                        badUrls.Value.Remove(item);
                    }
                }
            }
            //Console.WriteLine("recovery timer actived");
        }

        /// <summary>
        /// 获取客户端服务连接
        /// </summary>
        /// <param name="invocation">服务路径</param>
        /// <returns>客户端服务连接</returns>
        private IClientPool GetClientPool(IInvocation invocation)
        {

            //参数检查
            if (!Pools.ContainsKey(invocation.TargetType.FullName))
            {
                throw new Exception($"not find the {invocation.TargetType.FullName}'s pool,please config it ");
            }

            var clientPool = Pools[invocation.TargetType.FullName];
            clientPool.Address = Address;
            return clientPool;
        }
        /// <summary>
        /// 获取客户端缓存
        /// </summary>
        /// <param name="invocation">服务路径</param>
        /// <returns>客户端服务连接</returns>
        private ICache GetCache(IInvocation invocation)
        {
            //参数检查
            if (Caches == null)
            {
                return null;
            }

            ICache result = null;
            //app interface version
            var methodParameter = $"{invocation.AppPoint()}{invocation.TargetType.FullName}.{invocation.MethodInfo.Name}{invocation.PointVersion()}";
            var key = this.Address.GetMethodParameter(methodParameter, CACHE_KEY, "");
            //app interface
            if (string.IsNullOrEmpty(key))
            {
                key = this.Address.GetMethodParameter($"{invocation.AppPoint()}{invocation.TargetType.FullName}.{invocation.MethodInfo.Name}", CACHE_KEY, "");
            }
            //interface version
            if (string.IsNullOrEmpty(key))
            {
                key = this.Address.GetMethodParameter($"{invocation.TargetType.FullName}.{invocation.MethodInfo.Name}{invocation.PointVersion()}", CACHE_KEY, "");
            }
            //interface
            if (string.IsNullOrEmpty(key))
            {
                key = this.Address.GetMethodParameter($"{invocation.TargetType.FullName}.{invocation.MethodInfo.Name}", CACHE_KEY, "");
            }
            //key
            if (string.IsNullOrEmpty(key))
            {
                key = this.Address.GetParameter(CACHE_KEY, "");
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

            var key = this.Address.GetMethodParameter(invocation.MethodInfo.Name, LOADBANCE_KEY, "");

            if (string.IsNullOrEmpty(key) || key == RandomLoadBalance.NAME)
            {
                key = this.Address.GetInterfaceParameter(invocation.TargetType.FullName, LOADBANCE_KEY, "");
            }

            if (LoadBalances.ContainsKey(key) && key != RandomLoadBalance.NAME)
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

            var key = this.Address.GetMethodParameter(invocation.MethodInfo.Name, CLUSTER_KEY, "");

            if (string.IsNullOrEmpty(key) || key == FailoverCluster.NAME)
            {
                key = this.Address.GetInterfaceParameter(invocation.TargetType.FullName, CLUSTER_KEY, "");
            }
            if (Clusters.ContainsKey(key) && key != FailoverCluster.NAME)
            {
                result = Clusters[key];
            }

            //result.Pool = GetClientPool(invocation);
            //result.Address = this.Address;
            return result;
        }
        /// <summary>
        /// 根据配置获取路径集合
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>
        private IList<URL> GetUrls(IInvocation invocation)
        {
            //参数检查
            if (!Urls.ContainsKey(invocation.TargetType.FullName) && !BadUrls.ContainsKey(invocation.TargetType.FullName))
            {
                throw new Exception($"not find the {invocation.TargetType.FullName}'s urls,please config it ");
            }

            if (Urls?[invocation.TargetType.FullName]?.Count() > 0)
            {
                var result = filterUrls(invocation, Urls[invocation.TargetType.FullName]);
                if (result?.Count > 0)
                {
                    Console.WriteLine("from good urls");
                    return result;
                }
            }

            if (BadUrls?[invocation.TargetType.FullName]?.Count() > 0)
            {
                var result = filterUrls(invocation, BadUrls[invocation.TargetType.FullName].Select(w => w.Url).ToList());
                if (result?.Count > 0)
                {
                    Console.WriteLine("from bad urls");
                    return result;
                }
            }

            throw new Exception($"not find the {invocation.AppPoint()}{invocation.TargetType.FullName}{invocation.PointVersion()}'s urls,please config it,config version must <= Url version ");
        }

        /// <summary>
        /// 将url集合中没有设置App或者和调用App一致的URL集合取出
        /// </summary>
        /// <param name="invocation"></param>
        /// <param name="urls"></param>
        /// <returns></returns>
        private IList<URL> filterUrls(IInvocation invocation, IList<URL> urls)
        {
            var result = filterAppUrls(invocation, urls);
            result = filterVersionUrls(invocation, urls);
            return result;
        }
        /// <summary>
        /// 将url集合中没有设置App或者和调用App一致的URL集合取出
        /// </summary>
        /// <param name="invocation"></param>
        /// <param name="urls"></param>
        /// <returns></returns>
        private IList<URL> filterAppUrls(IInvocation invocation, IList<URL> urls)
        {
            if (string.IsNullOrEmpty(invocation.App) || (urls?.Count ?? 0) <= 0)
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
        private IList<URL> filterVersionUrls(IInvocation invocation, IList<URL> urls)
        {
            if (string.IsNullOrEmpty(invocation.Version) || (urls?.Count ?? 0) <= 0)
            {
                return urls;
            }

            var result = new List<URL>();
            foreach (var item in urls)
            {
                var version = item.GetParameter(URL.VERSION_KEY);
                if (string.IsNullOrWhiteSpace(version) || string.Compare(version, invocation.Version, true)>=0)
                {
                    result.Add(item);
                }
            }
            return result;
        }
        /// <summary>
        /// 执行远程调用
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>

        public IResult Invoke(IInvocation invocation)
        {
            //缓存
            var cache = this.GetCache(invocation);

            var parameters = invocation.MethodInfo.GetParameters();
            var key =$"{invocation.AppPoint()}{invocation.TargetType.FullName}.{invocation.MethodInfo.Name}@{StringUtils.Md5(StringUtils.ToArgumentString(parameters, invocation.Arguments))}{invocation.PointVersion()}";
            if (cache != null)
            {
                var value = cache.Get(key);
                if (value != null)
                {
                    Console.WriteLine($"call from cache:{key}");
                    Console.WriteLine($"cache type:{cache.GetType().FullName}");
                    return new RpcResult(value);
                }

                var resultInner = this.InvokeInner(invocation);
                if (!resultInner.HasException)
                {
                    cache.Put(key, resultInner.Value);
                }
                return resultInner;
            }

            var result = this.InvokeInner(invocation);
            return result;
        }

        /// <summary>
        /// clear all local cache 
        /// </summary>
        public void CacheClear()
        {
            if ((Caches?.Count??0)<=0)
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
        private IResult InvokeInner(IInvocation invocation)
        {
            var urls = this.GetUrls(invocation);

            //get the url address
            if (urls.Count <= 0)
            {
                throw new Exception($"there is no alive url to access the remote interface {invocation.TargetType.FullName}");
            }

            //get load balance
            var loadbalance = this.GetLoadBalance(invocation);

            var cluster = this.GetCluster(invocation);

            var pool = this.GetClientPool(invocation);

            var result = cluster.DoInvoke(pool, loadbalance, this.Address, urls, invocation);

            lock (locker)
            {
                IList<URL> goodUrls = new List<URL>();

                // insulate the exception rpc url address 
                IList<BadUrl> badUrls = new List<BadUrl>();
                if (this.Urls.ContainsKey(invocation.TargetType.FullName))
                {
                    goodUrls = this.Urls[invocation.TargetType.FullName];
                }

                if (this.BadUrls.ContainsKey(invocation.TargetType.FullName))
                {
                    badUrls = this.BadUrls[invocation.TargetType.FullName];
                }

                //get all bad url, insulate from good url
                foreach (var item in result.BadUrls)
                {
                    var goodUrl = goodUrls.FirstOrDefault(w => w == item.Url);
                    //remove from good urls ,add to bad urls
                    if (goodUrl != null)
                    {
                        goodUrls.Remove(goodUrl);

                        //refresh badurl timer
                        var badUrl = badUrls.FirstOrDefault(w => w.Url == goodUrl);
                        if (badUrl != null)
                        {
                            badUrls.Remove(badUrl);
                        }
                        Console.WriteLine($"isolation url {goodUrl.ToString()}");
                        badUrls.Add(item);
                    }
                }

                //扫描所有正常调用的地址，将隔离区的自动恢复到正常区域
                foreach (var item in result.Urls)
                {
                    var badUrl = badUrls.FirstOrDefault(w => w.Url == item);
                    //从隔离区删除,添加到正常区域
                    if (badUrl != null)
                    {
                        badUrls.Remove(badUrl);

                        if (!goodUrls.Contains(badUrl.Url))
                        {
                            goodUrls.Add(badUrl.Url);
                            Console.WriteLine($"recovery url {badUrl.ToString()}");
                            //logger.Error(badUrl.CurrentException);
                        }

                    }
                }

                if (!this.BadUrls.ContainsKey(invocation.TargetType.FullName) && badUrls.Count() > 0)
                {
                    this.BadUrls.TryAdd(invocation.TargetType.FullName, badUrls);
                }
            }
            
            //是否有异常，并抛出异常
            if (result.IsThrow && result.ClusterException!=null)
            {
                throw result.ClusterException;
            }

            return result.Result;
        }

    }

    
}
