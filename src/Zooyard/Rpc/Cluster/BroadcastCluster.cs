﻿using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Zooyard.Diagnositcs;

namespace Zooyard.Rpc.Cluster;

public class BroadcastCluster : AbstractCluster
{
    public BroadcastCluster(ILogger<BroadcastCluster> logger) : base(logger) { }
    public override string Name => NAME;
    public const string NAME = "broadcast";
    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, 
        ILoadBalance loadbalance,
        URL address,
        IList<URL> invokers,
        IList<BadUrl> disabledUrls,
        IInvocation invocation)
    {
        CheckInvokers(invokers, invocation, address);

        RpcContext.GetContext().SetInvokers(invokers);
        Exception? exception = null;
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();
        var isThrow = false;
        IResult<T>? result = null;
        foreach (var invoker in invokers)
        {
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
                exception = e;
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
                _logger.LogWarning(e, e.Message);
            }
            finally
            {
                watch.Stop();
            }
        }
        if (exception != null)
        {
            isThrow = true;
        }
        var clusterResult = new ClusterResult<T>(result, 
            goodUrls, badUrls,
            exception, isThrow);
        return clusterResult;
    }
}
