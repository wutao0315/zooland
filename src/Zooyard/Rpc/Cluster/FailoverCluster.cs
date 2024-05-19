using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using Zooyard.Attributes;
using Zooyard.Diagnositcs;
//using Zooyard.Logging;
using Zooyard.Utils;

namespace Zooyard.Rpc.Cluster;

public class FailoverCluster : AbstractCluster
{
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(FailoverCluster));
    //public FailoverCluster(IEnumerable<ICache> caches) : base(caches) { }
    public FailoverCluster(ILogger<FailoverCluster> logger) : base(logger) { }
    public override string Name => NAME;
    public const string NAME = "failover";
    public const string RETRIES_KEY = "retries";
    public const int DEFAULT_RETRIES = 2;

    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, 
        ILoadBalance loadbalance,
        URL address, 
        IList<URL> invokers, 
        IList<BadUrl> disabledUrls, 
        IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        var methodAttr = invocation.MethodInfo.GetCustomAttribute<RequestMappingAttribute>();

        CheckInvokers(invokers, invocation, address);

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

            var url = base.Select(loadbalance, invocation, invokers, disabledUrls, invoked);
            invoked.Add(url);
            RpcContext.GetContext().SetInvokers(invoked);

            var watch = Stopwatch.StartNew();
            try
            {
                var client = await pool.GetClient(url);

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                try
                {
                    var refer = await client.Refer();
                    _source.WriteConsumerBefore(client.System, Name, url, invocation);
                    var result = await refer.Invoke<T>(invocation);
                    result.ElapsedMilliseconds = watch.ElapsedMilliseconds;
                    _source.WriteConsumerAfter(client.System, Name, url, invocation, result);
                    await pool.Recovery(client);
                    if (le != null)
                    {
                        _logger.LogWarning(le, "Although retry the method " + invocation.MethodInfo.Name
                                + " in the service " + invocation.TargetType.FullName
                                + " was successful by the provider " + url.Address
                                + ", but there have been failed providers " + string.Join(",", providers)
                                + " (" + providers.Count + "/" + invokers.Count
                                + ") from the registry " + address.ToString().TrimEnd('/') + "/" + methodAttr?.Value?.TrimStart('/')
                                + " on the consumer " + Local.HostName
                                + " using the service version " + invocation.Version
                                + ". Last error is: " + le.Message);
                    }
                    goodUrls.Add(url);

                    return new ClusterResult<T>(result,
                        goodUrls, badUrls,
                        le, false);
                }
                catch (Exception ex)
                {
                    await pool.DestoryClient(client).ConfigureAwait(false);
                    _source.WriteConsumerError(client.System, Name, url, invocation, ex, watch.ElapsedMilliseconds);
                    throw;
                }
                

               
            }
            catch (RpcException e)
            {
                if (e.Biz)
                { // biz exception.
                    throw;
                }
                le = e;
            }
            catch (Exception e)
            {
                le = new RpcException(e.Message, e);

                //var badUrl = badUrls.FirstOrDefault(w => w.Url == url);
                //if (badUrl != null)
                //{
                //    badUrls.Remove(badUrl);
                //}
                //badUrls.Add(new BadUrl(url, le));

                var badUrlBase = disabledUrls.FirstOrDefault(w => w.Url == url);
                if (badUrlBase != null)
                {
                    badUrlBase.BadTime = DateTime.Now;
                    badUrlBase.CurrentException = le;
                }
                else
                {
                    disabledUrls.Add(new BadUrl(url, le));
                }

            }
            finally
            {
                providers.Add(url.Address!); 
                watch.Stop();
            }
        }

         var re = new RpcException(le != null ? le.Code : 0, "Failed to invoke the method "
               + invocation.MethodInfo.Name + " in the service " + invocation.TargetType.FullName
               + ". Tried " + len + " times of the providers " + string.Join(",", providers)
               + " (" + providers.Count + "/" + invokers.Count
               + ") from the registry " + address.ToString().TrimEnd('/') + "/" + methodAttr?.Value?.TrimStart('/')
                + " on the consumer " + Local.HostName
               + " using the service version " + invocation.Version 
               + ". Last error is: "
               + (le != null ? le.Message : ""), le != null && le.InnerException != null ? le.InnerException : le);

        return new ClusterResult<T>(new RpcResult<T>(re), 
            goodUrls, badUrls, 
            re, true);
       
    }
}


