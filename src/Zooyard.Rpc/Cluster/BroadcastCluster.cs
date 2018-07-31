using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Cluster
{
    public class BroadcastCluster : AbstractCluster
    {
        public const string NAME = "broadcast";
        public override IClusterResult DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            checkInvokers(urls, invocation);
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
                        result = refer.Invoke(invocation);
                        pool.Recovery(client);
                        goodUrls.Add(invoker);
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
                    logger.Warn(e.Message, e);
                }
            }
            if (exception != null)
            {
                isThrow = true;
            }
            var clusterResult = new ClusterResult(result, goodUrls, badUrls,exception, isThrow);
            return clusterResult;
        }
    }
}
