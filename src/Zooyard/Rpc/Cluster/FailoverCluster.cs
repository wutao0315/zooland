using Zooyard.Diagnositcs;
using Zooyard.Logging;
using Zooyard.Utils;

namespace Zooyard.Rpc.Cluster;

public class FailoverCluster : AbstractCluster
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(FailoverCluster));
    //public FailoverCluster(IEnumerable<ICache> caches) : base(caches) { }
    public override string Name => NAME;
    public const string NAME = "failover";
    public const string RETRIES_KEY = "retries";
    public const int DEFAULT_RETRIES = 2;

    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> invokers, IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();

        CheckInvokers(invokers, invocation, address);

        ////路由
        //var invokers = base.Route(urls, address, invocation);

        //*getUrl();
        int len = address.GetMethodParameter(invocation.MethodInfo.Name, RETRIES_KEY, DEFAULT_RETRIES) + 1;
        if (len <= 0)
        {
            len = 1;
        }
        // retry loop.
        RpcException? le = null; // last exception.
        var invoked = new List<URL>(invokers.Count); // invoked invokers.
        ISet<string> providers = new HashSet<string>();//*len
        for (int i = 0; i < len; i++)
        {
            //重试时，进行重新选择，避免重试时invoker列表已发生变化.
            //注意：如果列表发生了变化，那么invoked判断会失效，因为invoker示例已经改变
            if (i > 0)
            {
                // checkWhetherDestroyed();
                // copyinvokers = list(invocation);
                //重新检查一下
                CheckInvokers(invokers, invocation, address);
            }

            var url = base.Select(loadbalance, invocation, invokers, invoked);
            invoked.Add(url);
            RpcContext.GetContext().SetInvokers(invoked);
            
            try
            {
                var client = await pool.GetClient(url);
                try
                {
                    var refer = await client.Refer();
                    _source.WriteConsumerBefore(refer.Instance, url, invocation);
                    var result = await refer.Invoke<T>(invocation);
                    _source.WriteConsumerAfter(url, invocation, result);
                    await pool.Recovery(client);
                    if (le != null)
                    {
                        Logger().LogWarning(le, "Although retry the method " + invocation.MethodInfo.Name
                                + " in the service " + invocation.TargetType.FullName
                                + " was successful by the provider " + url.Address
                                + ", but there have been failed providers " + string.Join(",", providers)
                                + " (" + providers.Count + "/" + invokers.Count
                                + ") from the registry " + address.Address
                                + " on the consumer " + Local.HostName
                                + " using the service version " + invocation.Version
                                + ". Last error is: " + le.Message);
                    }
                    goodUrls.Add(url);

                    return new ClusterResult<T>(result, goodUrls, badUrls, le, false);
                }
                catch (Exception ex)
                {
                    await pool.DestoryClient(client).ConfigureAwait(false);
                    _source.WriteConsumerError(url, invocation, ex);
                    throw;
                }
                

               
            }
            catch (RpcException e)
            {
                if (e.Biz)
                { // biz exception.
                    throw e;
                }
                le = e;
            }
            catch (Exception e)
            {
                le = new RpcException(e.Message, e);

                var badUrl = badUrls.FirstOrDefault(w => w.Url == url);
                if (badUrl != null)
                {
                    badUrls.Remove(badUrl);
                }
                badUrls.Add(new BadUrl { Url = url, BadTime = DateTime.Now, CurrentException = le });
                
            }
            finally
            {
                providers.Add(url.Address);
            }
        }

         var re = new RpcException(le != null ? le.Code : 0, "Failed to invoke the method "
               + invocation.MethodInfo.Name + " in the service " + invocation.TargetType.FullName
               + ". Tried " + len + " times of the providers " + string.Join(",", providers)
               + " (" + providers.Count + "/" + invokers.Count
               + ") from the registry " + address
               //+ " on the consumer " + NetUtils.getLocalHost() 
               + " using the service version " + invocation.Version 
               + ". Last error is: "
               + (le != null ? le.Message : ""), le != null && le.InnerException != null ? le.InnerException : le);

        return new ClusterResult<T>(new RpcResult<T>(re), goodUrls, badUrls, re, true);
       
    }
}


