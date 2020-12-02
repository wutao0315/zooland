using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Diagnositcs;
using Zooyard.Core.Logging;
using Zooyard.Core.Utils;

namespace Zooyard.Rpc.Cluster
{
    public class FailfastCluster : AbstractCluster
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(FailfastCluster));

        public override string Name => NAME;
        public const string NAME = "failfast";


        public override async Task<IClusterResult> DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            IResult result = null;
            Exception exception = null;
            var isThrow = false;

            checkInvokers(urls, invocation, address);
            var invoker = base.select(loadbalance, invocation, urls, null);

            try
            {
                var client =await pool.GetClient(invoker);
                try
                {
                    var refer = await client.Refer();
                    _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                    result = await refer.Invoke(invocation);
                    _source.WriteConsumerAfter(invoker, invocation, result);
                    pool.Recovery(client);
                    goodUrls.Add(invoker);
                }
                catch (Exception ex)
                {
                    await pool.DestoryClient(client).ConfigureAwait(false);
                    _source.WriteConsumerError(invoker,invocation ,ex);
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
                    + " on consumer " + Local.HostName
                    + " use service version " + invocation.Version
                    + ", but no luck to perform the invocation. Last error is: " + e.Message, e.InnerException != null ? e.InnerException : e);
                }
                Logger().LogError(exception, exception.Message);
                badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
            }

            return new ClusterResult(result, goodUrls, badUrls, exception, isThrow);

        }
    }
}
