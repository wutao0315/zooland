using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Atomic;
using Zooyard.Core.Diagnositcs;

namespace Zooyard.Rpc.Cluster
{
    public class ForkingCluster : AbstractCluster
    {
        public override string Name => NAME;
        public const string NAME = "forking";
        public const string FORKS_KEY = "forks";
        public const int DEFAULT_FORKS = 2;

        private readonly ILogger _logger;
        public ForkingCluster(ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ForkingCluster>();
        }

        public override IClusterResult DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            //IResult result = null;
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();

            checkInvokers(urls, invocation, address);
            IList<URL> selected;

            int forks = address.GetParameter(FORKS_KEY, DEFAULT_FORKS);
            int timeout = address.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);

            if (forks <= 0 || forks >= urls.Count)
            {
                selected = urls;
            }
            else
            {
                selected = new List<URL>();
                for (int i = 0; i < forks; i++)
                {
                    //在invoker列表(排除selected)后,如果没有选够,则存在重复循环问题.见select实现.
                    var invoker = base.select(loadbalance, invocation, urls, selected);
                    if (!selected.Contains(invoker))
                    {//防止重复添加invoker
                        selected.Add(invoker);
                    }
                }
            }
            RpcContext.GetContext().SetInvokers(selected);
            var count = new AtomicInteger();

            var taskList = new Task<IResult>[selected.Count];
            var index = 0;
            foreach (var invoker in selected)
            {
                var task = Task.Run<IResult>(() => {
                    try
                    {
                        var client = pool.GetClient(invoker);
                        try
                        {
                            var refer = client.Refer();
                            _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                            var resultInner = refer.Invoke(invocation);
                            _source.WriteConsumerAfter(invoker, invocation, resultInner);
                            pool.Recovery(client);
                            goodUrls.Add(invoker);
                            return resultInner;
                        }
                        catch (Exception ex)
                        {
                            pool.DestoryClient(client);
                            _source.WriteConsumerError(invoker,invocation ,ex);
                            throw ex;
                        }
                    }
                    catch (Exception e)
                    {
                        badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = e });
                        int value = count.IncrementAndGet();
                        if (value >= selected.Count)
                        {
                            return new RpcResult(e);
                        }
                    }
                    return null;
                });
                taskList[index++] = task;
            }
            try
            {
                var retIndex=Task.WaitAny(taskList,timeout);
                var ret= taskList[retIndex].Result;
                if (ret.HasException)
                {
                    Exception e = ret.Exception;
                    throw new RpcException(e is RpcException? ((RpcException) e).Code : 0, "Failed to forking invoke provider " + selected + ", but no luck to perform the invocation. Last error is: " + e.Message, e.InnerException != null ? e.InnerException : e);
                }
                return new ClusterResult(ret, goodUrls, badUrls, null,false);
            }
            catch (Exception e)
            {
                var exception = new RpcException("Failed to forking invoke provider " + selected + ", but no luck to perform the invocation. Last error is: " + e.Message, e);
                return new ClusterResult(new RpcResult(exception), goodUrls, badUrls, exception, true);
            }
        }
    }
}
