using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Diagnositcs;
using Zooyard.Core.Logging;

namespace Zooyard.Rpc.Cluster
{
    public class FailsafeCluster : AbstractCluster
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(FailsafeCluster));
        public override string Name => NAME;
        public const string NAME = "failsafe";


        public override async Task<IClusterResult> DoInvoke(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            Exception exception = null;
            checkInvokers(urls, invocation, address);
            var invoker = base.select(loadbalance, invocation, urls, null);
            IResult result;
            try
            {
                var client = pool.GetClient(invoker);
                try
                {
                    var refer = await client.Refer();
                    _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                    result = await refer.Invoke(invocation);
                    _source.WriteConsumerAfter(invoker, invocation, result);
                    pool.Recovery(client);
                    goodUrls.Add(invoker);
                    return new ClusterResult(result, goodUrls, badUrls, exception, false);
                }
                catch (Exception ex)
                {
                    await pool.DestoryClient(client).ConfigureAwait(false);
                    _source.WriteConsumerError(invoker, invocation, ex);
                    throw ex;
                }
            }
            catch (Exception e)
            {
                exception = e;
                badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = exception });
                Logger().Error(e, $"Failsafe ignore exception: {e.Message}");
                result = new RpcResult(e); // ignore
            }
            return new ClusterResult(result, goodUrls, badUrls,exception,false);
        }
    }
}
