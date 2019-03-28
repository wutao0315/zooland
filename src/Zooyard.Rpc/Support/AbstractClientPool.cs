using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractClientPool : IClientPool
    {
        private readonly ILogger _logger;
        #region 构造方法
        public AbstractClientPool(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AbstractClientPool>();
            //init
            CreateResetEvent();
            CreatePool();
        }

        #endregion

        #region 内部成员
        /// <summary>
        /// client pools
        /// </summary>
        protected ConcurrentDictionary<string, ConcurrentQueue<IClient>> ClientsPool;
        /// <summary>
        /// 同步连接
        /// </summary>
        protected AutoResetEvent resetEvent;

        /// <summary>
        /// 空闲连接数
        /// </summary>
        protected volatile ConcurrentDictionary<string, int> idleCount = new ConcurrentDictionary<string, int>();

        //protected volatile int idleCount = 0;

        /// <summary>
        /// 活动连接数
        /// </summary>
        protected volatile ConcurrentDictionary<string, int> activeCount = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// 同步连接锁
        /// </summary>		
        protected object locker = new object();

        /// <summary>
        /// 释放标志
        /// </summary>
        protected bool disposed;
        /// <summary>
        /// 随机数生成器
        /// </summary>
        protected Random rand = new Random();
        #endregion

        #region 属性
        public URL Address { get; set; }

        /// <summary>
        /// 连接池最大活动连接数
        /// </summary>
        public int MaxActive { protected set; get; }
        /// <summary>
        /// 连接池最小空闲连接数
        /// </summary>
        public int MinIdle { protected set; get; }
        /// <summary>
        /// 连接池最大空闲连接数
        /// </summary>
        public int MaxIdle { protected set; get; }
        /// <summary>
        /// 通信超时时间，单位毫秒
        /// </summary>
        public int ClientTimeout { protected set; get; }

        /// <summary>
        /// 空闲连接数
        /// </summary>
        public IDictionary<string, int> IdleCount
        {
            get
            {
                return idleCount;
            }
        }

        /// <summary>
        /// 活动连接数
        /// </summary>
        public IDictionary<string, int> ActiveCount
        {
            get
            {
                return activeCount;
            }
        }
        #endregion

        

        #region 公有操作方法

        /// <summary>
        /// 从连接池取出一个连接
        /// </summary>
        /// <returns>连接</returns>
        public virtual IClient GetClient(URL url)
        {
            var urlKey = url.ToString();
            if (Monitor.TryEnter(locker, TimeSpan.FromMilliseconds(ClientTimeout)))
            {
                try
                {
                    IClient client = null;
                    Exception innerErr = null;
                    var validClient = false;
                    //连接池无空闲连接	
                    
                    if (idleCount.ContainsKey(urlKey) && idleCount[urlKey] > 0 && !validClient)
                    {
                        client = DequeueClient(urlKey);
                        validClient = ValidateClient(client, out innerErr);
                        if (!validClient)
                        {
                            DestoryClient(client);
                        }
                        _logger.LogInformation($"get client [{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}] from queue");
                    }

                    //连接池无空闲连接	
                    if (!validClient)
                    {
                        //连接池已已创建连接数达上限				
                        if (idleCount.ContainsKey(urlKey) && activeCount[urlKey] > MaxActive)
                        {
                            if (!resetEvent.WaitOne(ClientTimeout))
                            {
                                throw new TimeoutException("the pool is busy,no available connections.");
                            }
                        }
                        else
                        {
                            client = InitializeClient(url, out innerErr);
                            if (client == null)
                            {
                                throw new InvalidOperationException("connection access failed. please confirm call service status.", innerErr);
                            }
                            _logger.LogInformation($"create new client [{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
                        }
                    }

                    return client;
                }
                finally
                {
                    Monitor.Exit(locker);
                }
            }
            else
            {
                throw new TimeoutException($"gets the connection wait more than {ClientTimeout} milliseconds.");
            }
        }

        /// <summary>
        /// 归还一个连接至连接池
        /// </summary>
        /// <param name="client">连接</param>
        public virtual void Recovery(IClient client)
        {
            lock (locker)
            {
                var urlKey = client.Url.ToString();
                //空闲连接数达到上限或者连接版本过期，不再返回线程池,直接销毁			
                if ((idleCount.ContainsKey(urlKey)
                    && idleCount[urlKey] >= MaxIdle))//|| this.Version != client.Version
                {
                    _logger.LogInformation($"recovery to destory idle overflow:[{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
                    DestoryClient(client);
                    Console.WriteLine($"recovery to destory idle overflow:[{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
                }
                else
                {
                    //更新最近触发时间
                    client.ActiveTime = DateTime.Now;
                    //连接回归连接池
                    EnqueueClient(urlKey, client);
                    //发通知信号，连接池有连接变动
                    resetEvent.Set();
                    _logger.LogInformation($"recovery to update:[{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
                    Console.WriteLine($"recovery to update:[{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
                }
            }
        }
        /// <summary>
        /// 销毁连接
        /// </summary>
        /// <param name="client">连接</param>
        public void DestoryClient(IClient client)
        {
            if (client != null)
            {
                var urlKey = client.Url.ToString();
                client.Close();
                client.Dispose();
                activeCount[urlKey]--;
                _logger.LogInformation($"DestoryClient :[{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
            }
        }
        /// <summary>
        /// 重置连接池
        /// </summary>
        public virtual void ResetPool()
        {
            CreatePool();
        }

        /// <summary>
        /// 释放连接池
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region 私有方法

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                lock (locker)
                {
                    foreach (var item in ClientsPool.Keys)
                    {
                        while (idleCount[item] > 0)
                        {
                            var client = DequeueClient(item);
                            var urlKey = client.Url.ToString();
                            _logger.LogInformation($"Dispose :[{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
                            DestoryClient(client);
                        }
                    }
                }
            }
            disposed = true;
        }


        /// <summary>
        /// 创建线程同步对象
        /// </summary>
        protected virtual void CreateResetEvent()
        {
            lock (locker)
            {
                if (resetEvent == null)
                {
                    resetEvent = new AutoResetEvent(false);
                }
            }
        }

        /// <summary>
        /// 创建连接池
        /// </summary>

        protected virtual void CreatePool()
        {
            lock (locker)
            {
                //读取配置
                MaxActive = 100;
                MinIdle = 2;
                MaxIdle = 10;
                ClientTimeout = 5000;

                if (ClientsPool == null)
                {
                    ClientsPool = new ConcurrentDictionary<string,ConcurrentQueue<IClient>>();
                }
            }
        }


        /// <summary>
        /// 连接进入连接池
        /// </summary>
        /// <param name="client">连接</param>
        protected void EnqueueClient(string url,IClient client)
        {
            if (!ClientsPool.ContainsKey(url))
            {
                ClientsPool.TryAdd(url, new ConcurrentQueue<IClient>());
            }
            ClientsPool[url].Enqueue(client);
            idleCount[url]++;
            activeCount[url]--;
        }

        /// <summary>
        /// 连接取出连接池
        /// </summary>
        /// <returns>连接</returns>
        protected IClient DequeueClient(string url)
        {
            IClient client;
            if (ClientsPool[url].TryDequeue(out client))
            {
                idleCount[url]--;
                activeCount[url]++;
            }
            return client;
        }

        /// <summary>
        /// 创建一个连接，虚函数，应由特定连接池继承
        /// </summary>
        /// <returns>连接</returns>
        protected abstract IClient CreateClient(URL url);

        /// <summary>
        /// 初始化连接，隐藏创建细节
        /// </summary>
        /// <returns>连接</returns>
        protected IClient InitializeClient(URL url,out Exception err)
        {
            err = null;

            try
            {
                var client = CreateClient(url);
                if (ValidateClient(client, out err))
                {
                    var urlKey = url.ToString();
                    if (!activeCount.ContainsKey(urlKey))
                    {
                        activeCount.TryAdd(urlKey, 0);
                    }
                    if (!idleCount.ContainsKey(urlKey))
                    {
                        idleCount.TryAdd(urlKey, 0);
                    }
                    activeCount[urlKey]++;
                    client.Reset();

                    return client;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,e.Message);
                err = e;
            }
            
            return null;
        }

        /// <summary>
        /// 校验连接，确保连接开启
        /// </summary>
        /// <param name="client">连接</param>

        protected bool ValidateClient(IClient client, out Exception err)
        {
            try
            {
                client.Open();
                err = null;
                return true;
            }
            catch (Exception e)
            {
                err = e;
                _logger.LogError(e, e.Message);
                return false;
            }
        }

        
        /// <summary>
        /// 超时清除
        /// </summary>
        /// <param name="overTime"></param>
        public void TimeOver(DateTime overTime)
        {
            foreach (var item in ClientsPool.Keys)
            {
                var list = new List<IClient>();
                while (idleCount[item]>0)
                {
                    var client = DequeueClient(item);
                    if (client==null)
                    {
                        continue;
                    }
                    var urlKey = client.Url.ToString();
                    if (client.ActiveTime<=DateTime.MinValue || client.ActiveTime < overTime)
                    {
                        Console.WriteLine($"client time over:[{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
                        _logger.LogInformation($"client time over:[{idleCount[urlKey]}][{activeCount[urlKey]}][{client.Version}:{urlKey}]");
                        DestoryClient(client);
                    }
                    else
                    {
                        list.Add(client);
                    }
                }
                foreach (var client in list)
                {
                    EnqueueClient(item, client);
                }
            }

            PrintConsole();
        }

        #endregion

        private void PrintConsole()
        {
            //Console.WriteLine($"client pool information:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss.fff")}");
            //Console.WriteLine("-----------------------------------------");
            //Console.WriteLine("idle|active|pool|url");
            //foreach (var pool in ClientsPool)
            //{
            //    var idle = idleCount.ContainsKey(pool.Key) ? idleCount[pool.Key] :-1;
            //    var active = activeCount.ContainsKey(pool.Key) ? activeCount[pool.Key] : -1;
            //    Console.WriteLine($"{idle}|{active}|{pool.Value?.Count ?? 0}|{pool.Key}");
            //}
            //Console.WriteLine("-----------------------------------------");
        }
    }
}
