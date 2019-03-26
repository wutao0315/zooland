using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Zooyard.Core;
using Zooyard.Core.Diagnositcs;

namespace Zooyard.Rpc.Cluster
{
    public class BroadcastCluster : AbstractCluster
    {
        private readonly ILogger _logger;
        public BroadcastCluster(ILoggerFactory loggerFactory)
            :base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BroadcastCluster>();
        }
        public override string Name => NAME;
        public const string NAME = "broadcast";
        public override IClusterResult DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            checkInvokers(urls, invocation, address);
            RpcContext.GetContext().SetInvokers(urls);
            Exception exception = null;
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            var isThrow = false;
            IResult result = null;
            foreach (var invoker in urls)
            {
                try
                {
                    var client = pool.GetClient(invoker);
                    try
                    {
                        var refer = client.Refer();
                        _source.WriteConsumerBefore(invoker, invocation);
                        result = refer.Invoke(invocation);
                        _source.WriteConsumerAfter(invoker, invocation, result);
                        pool.Recovery(client);
                        goodUrls.Add(invoker);
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
                    exception = e;
                    badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
                    _logger.LogWarning(e, e.Message);
                }
            }
            if (exception != null)
            {
                isThrow = true;
            }
            var clusterResult = new ClusterResult(result, goodUrls, badUrls, exception, isThrow);
            return clusterResult;
        }
    }
}
