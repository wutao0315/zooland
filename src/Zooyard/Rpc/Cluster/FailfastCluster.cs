using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Security.Policy;
using Zooyard.Diagnositcs;
//using Zooyard.Logging;
using Zooyard.Utils;

namespace Zooyard.Rpc.Cluster;

public class FailfastCluster : AbstractCluster
{
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(FailfastCluster));
    //public FailfastCluster(IEnumerable<ICache> caches) : base(caches) { }
    public FailfastCluster(ILogger<FailfastCluster> logger) : base(logger) { }
    public override string Name => NAME;
    public const string NAME = "failfast";


    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, 
        ILoadBalance loadbalance, 
        URL address, 
        IList<URL> invokers,
        IList<BadUrl> disabledUrls,
        IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        IResult<T>? result = null;
        Exception? exception = null;
        var isThrow = false;

        CheckInvokers(invokers, invocation, address);

        var invoker = base.Select(address, loadbalance, invocation, invokers, disabledUrls);

        var watch = Stopwatch.StartNew();
        try
        {
            var client = await pool.GetClient(invoker);

            try
            {
                var refer = await client.Refer();
                _source.WriteConsumerBefore(client.System, Name, invoker, invocation);
                result = await refer.Invoke<T>(invocation);
                result.ElapsedMilliseconds = watch.ElapsedMilliseconds;
                _source.WriteConsumerAfter(client.System, Name, invoker, invocation, result);
                await pool.Recovery(client);
                goodUrls.Add(invoker);
            }
            catch (Exception ex)
            {
                await pool.DestoryClient(client);
                _source.WriteConsumerError(client.System, Name, invoker, invocation, ex, watch.ElapsedMilliseconds);
                throw;
            }
        }
        catch (Exception e)
        {
            isThrow = true;

            if (e is RpcException eBiz && eBiz.Biz)
            { 
                // biz exception.
                exception = e;
                //throw (RpcException)e;
            }
            else {

                exception = new RpcException(e is RpcException eRpc ? eRpc.Code : 0, "Failfast invoke providers "
                + invoker + " " + loadbalance.GetType().Name
                + " select from all providers " + string.Join(",", invokers)
                + " for service " + invocation.TargetType.FullName
                + " method " + invocation.MethodInfo.Name
                + " on consumer " + Local.HostName
                + " use service version " + invocation.Version
                + ", but no luck to perform the invocation. Last error is: " + e.Message+e.StackTrace, e.InnerException ?? e);
            }
            _logger.LogError(exception, exception.Message);
            var badUrl = badUrls.FirstOrDefault(w => w.Url == invoker);
            if (badUrl != null)
            {
                badUrl.BadTime = DateTime.Now;
                badUrl.CurrentException = e;
            }
            else
            {
                badUrls.Add(new BadUrl(invoker, e));
            }
        }
        finally
        {
            watch.Stop();
        }

        return new ClusterResult<T>(result, 
            goodUrls, badUrls,
            exception, isThrow);

    }
}
