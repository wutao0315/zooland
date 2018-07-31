using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Cluster
{
    public class FailfastCluster : AbstractCluster
    {
        public const string NAME = "failfast";
        public override IClusterResult DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            IResult result = null;
            Exception exception = null;
            var isThrow = false;

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
                }
                catch (Exception ex)
                {

                    pool.Recovery(client);
                    throw ex;
                }
            }
            catch (Exception e)
            {
                isThrow = true;

                if (e is RpcException && ((RpcException)e).Biz)
                { // biz exception.
                    exception = e;
                    //throw (RpcException)e;
                }
                else {

                    exception = new RpcException(e is RpcException ? ((RpcException)e).Code : 0, "Failfast invoke providers "
                    + invoker + " " + loadbalance.GetType().Name
                    + " select from all providers " + string.Join(",", urls)
                    + " for service " + invocation.TargetType.FullName
                    + " method " + invocation.MethodInfo.Name
                    //+ " on consumer " + NetUtils.getLocalHost()
                    //+ " use zooyard version " + Version.getVersion()
                    + ", but no luck to perform the invocation. Last error is: " + e.Message, e.InnerException != null ? e.InnerException : e);
                }

                badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
            }

            return new ClusterResult(result, goodUrls, badUrls, exception, isThrow);

        }
    }
}
