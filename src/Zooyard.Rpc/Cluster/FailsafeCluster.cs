using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Cluster
{
    public class FailsafeCluster : AbstractCluster
    {
        public const string NAME = "failsafe";
        public override IClusterResult DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            IResult result = null;
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            Exception exception = null;
            checkInvokers(urls, invocation);
            var invoker = base.select(loadbalance, invocation, urls, null);
            try
            {
                var client = pool.GetClient(invoker);
                try
                {
                    var refer = client.Refer();
                    result = refer.Invoke(invocation);
                    pool.Recovery(client);
                    goodUrls.Add(invoker);
                    return new ClusterResult(result, goodUrls, badUrls, exception,false);
                }
                catch (Exception ex)
                {
                    pool.Recovery(client);
                    throw ex;
                }
            }
            catch (Exception e)
            {
                exception = e;
                badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
                logger.Error("Failsafe ignore exception: " + e.Message, e);
                result = new RpcResult(e); // ignore
            }
            return new ClusterResult(result, goodUrls, badUrls,exception,false);
        }
    }
}
