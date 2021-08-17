using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Zooyard;
using Zooyard.Logging;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractClientPool : IClientPool,IAsyncDisposable
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(AbstractClientPool));

        #region 内部成员
        /// <summary>
        /// client pools
        /// </summary>
        protected readonly ConcurrentDictionary<URL, ConcurrentBag<IClient>> ClientsPool = new ConcurrentDictionary<URL, ConcurrentBag<IClient>> ();
       
        #endregion

        #region 属性
        public URL Address { get; set; }
        public int MaxIdle { protected set; get; } = Environment.ProcessorCount * 2;
        #endregion

        #region 公有操作方法

        /// <summary>
        /// 从连接池取出一个连接
        /// </summary>
        /// <returns>连接</returns>
        public virtual async Task<IClient> GetClient(URL url)
        {
            //连接池无空闲连接	
            var client = DequeueClient(url);
            var validClient = await ValidateClient(client);
            //连接池无空闲连接	
            if (!validClient)
            {
                //先尝试关闭
                await DestoryClient(client);
                //然后重新初始化
                client = await InitializeClient(url);
                if (client == null)
                {
                    throw new InvalidOperationException("connection access failed. please confirm call service status.");
                }
                Logger().LogInformation($"create new client [{client.Version}:{url}]");
            }

            return client;
        }

        /// <summary>
        /// 归还一个连接至连接池
        /// </summary>
        /// <param name="client">连接</param>
        public virtual async Task Recovery(IClient client)
        {
            if (!ClientsPool.TryGetValue(client.Url, out ConcurrentBag<IClient> clientBag) || clientBag.Count <= MaxIdle)
            {
                //更新最近触发时间
                client.ActiveTime = DateTime.Now;
                //连接回归连接池
                clientBag ??= new ConcurrentBag<IClient>();
                clientBag.Add(client);
                ClientsPool[client.Url] = clientBag;
                Logger().LogInformation($"recovery to update:[{clientBag.Count}][{client.Version}:{client.Url}]");
                return;
            }
            //空闲连接数达到上限或者连接版本过期，不再返回线程池,直接销毁	
            await DestoryClient(client);
            Logger().LogInformation($"recovery to destory idle full:[{clientBag.Count}][{client.Version}:{client.Url}]");
        }
        /// <summary>
        /// 销毁连接
        /// </summary>
        /// <param name="client">连接</param>
        public async Task DestoryClient(IClient client)
        {
            if (client == null)
            {
                return;
            }
            await client.Close();
            await client.DisposeAsync();
            Logger().LogInformation($"DestoryClient :[{client.Version}:{client.Url}]");
        }

        public virtual async ValueTask DisposeAsync()
        {
            foreach (var item in ClientsPool)
            {
                while (item.Value.TryTake(out IClient client)) 
                {
                    await DestoryClient(client);
                    Logger().LogInformation($"Dispose :[{ClientsPool[item.Key].Count}][{client.Version}:{item.Key}]");
                }
            }
        }

        /// <summary>
        /// 连接取出连接池
        /// </summary>
        /// <returns>连接</returns>
        protected IClient DequeueClient(URL url)
        {
            if (ClientsPool.TryGetValue(url, out ConcurrentBag<IClient> clients) && clients.TryTake(out IClient result)) 
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// 创建一个连接，虚函数，应由特定连接池继承
        /// </summary>
        /// <returns>连接</returns>
        protected abstract Task<IClient> CreateClient(URL url);

        /// <summary>
        /// 初始化连接，隐藏创建细节
        /// </summary>
        /// <returns>连接</returns>
        protected async Task<IClient> InitializeClient(URL url)
        {
            try
            {
                var client = await CreateClient(url);
                if (await ValidateClient(client))
                {
                    client.Reset();
                    return client;
                }
            }
            catch (Exception e)
            {
                Logger().LogError(e, e.Message);
            }
            return null;
        }

        /// <summary>
        /// 校验连接，确保连接开启
        /// </summary>
        /// <param name="client">连接</param>
        protected async Task<bool> ValidateClient(IClient client)
        {
            if (client == null) 
            {
                return false;
            }
            try
            {
                await client.Open();
                return true;
            }
            catch (Exception e)
            {
                Logger().LogError(e, e.Message);
                return false;
            }
        }


        /// <summary>
        /// 超时清除
        /// </summary>
        /// <param name="overTime"></param>
        public async Task TimeOver(DateTime overTime)
        {
            foreach (var item in ClientsPool)
            {
                var list = new List<IClient>();
                while (item.Value.TryTake(out IClient client)) 
                {
                    if (client.ActiveTime <= DateTime.MinValue || client.ActiveTime < overTime)
                    {
                        Logger().LogInformation($"client time over:[{item.Value.Count}][{client.Version}:{item.Key}]");
                        await DestoryClient(client);
                    }
                    else
                    {
                        list.Add(client);
                    }
                }
                foreach (var client in list)
                {
                    item.Value.Add(client);
                }
            }
#if DEBUG
            PrintConsole();
            void PrintConsole()
            {

                Console.WriteLine($"client pool information:{DateTime.Now:yyyy-MM-dd HH:mm:sss.fff}");
                Console.WriteLine("-----------------------------------------");
                Console.WriteLine("url|idle");
                foreach (var pool in ClientsPool)
                {
                    Console.WriteLine($"{pool.Key}|{pool.Value?.Count ?? 0}");
                }
                Console.WriteLine("-----------------------------------------");

            }
#endif
        }

        #endregion
    }
}
