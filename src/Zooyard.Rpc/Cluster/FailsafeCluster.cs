using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Zooyard.Core;
using Zooyard.Core.Diagnositcs;

namespace Zooyard.Rpc.Cluster
{
    public class FailsafeCluster : AbstractCluster
    {
        public override string Name => NAME;
        public const string NAME = "failsafe";
        private readonly ILogger _logger;
        public FailsafeCluster(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FailsafeCluster>();
        }

        public override IClusterResult DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            IResult result = null;
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            Exception exception = null;
            checkInvokers(urls, invocation, address);
            var invoker = base.select(loadbalance, invocation, urls, null);
            try
            {
                var client = pool.GetClient(invoker);
                try
                {
                    var refer = client.Refer();
                    _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                    result = refer.Invoke(invocation);
                    _source.WriteConsumerAfter(invoker, invocation, result);
                    pool.Recovery(client);
                    goodUrls.Add(invoker);
                    return new ClusterResult(result, goodUrls, badUrls, exception,false);
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
                _logger.LogError(e, $"Failsafe ignore exception: {e.Message}");
                result = new RpcResult(e); // ignore
            }
            return new ClusterResult(result, goodUrls, badUrls,exception,false);
        }
    }
}
