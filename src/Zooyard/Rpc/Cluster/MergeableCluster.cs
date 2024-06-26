﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using Zooyard.Diagnositcs;
using Zooyard.Rpc.Merger;

namespace Zooyard.Rpc.Cluster;

public class MergeableCluster : AbstractCluster
{
    public override string Name => NAME;
    public const string NAME = "mergeable";
    public const string MERGER_KEY = "merger";

    private readonly IDictionary<Type, IMerger> _defaultMergers;
    //private readonly IDictionary<string, IMerger> _mySelfMergers;
    public MergeableCluster(ILogger<MergeableCluster> logger,
        //IOptionsMonitor<ZooyardOption> zooyard,
        IEnumerable<IMerger> defaultMergers):base(logger)
    {
        _defaultMergers = new Dictionary<Type, IMerger>();
        foreach (var merge in defaultMergers)
        {
            _defaultMergers[merge.Type] = merge;
        }

        //_mySelfMergers = new Dictionary<string, IMerger>();
        //foreach (var item in zooyard.CurrentValue.Mergers)
        //{
        //    var merger = _defaultMergers.Values.FirstOrDefault(w=>w.Name == item);
        //    if (merger == null) 
        //    {
        //        continue;
        //    }
        //    _mySelfMergers.Add(item, merger);
        //}
       
    }

    private string GetGroupDescFromServiceKey(string key)
    {
        int index = key.IndexOf('/');
        if (index > 0)
        {
            return $"group [ {key.Substring(0, index)} ]";
        }
        return key;
    }
    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, 
        ILoadBalance loadbalance, 
        URL address,
        IList<URL> invokers, 
        IList<BadUrl> disabledUrls, 
        IInvocation invocation)
    {
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();

        var merger = address.GetMethodParameter(invocation.MethodInfo.Name, MERGER_KEY);
        // If a method doesn't have a merger, only invoke one Group
        if (string.IsNullOrEmpty(merger))
        {
            foreach (var invoker in invokers)
            {
                var watchInner = Stopwatch.StartNew();
                try
                {
                    var client = await pool.GetClient(invoker);
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    try
                    {
                        var refer = await client.Refer();
                        _source.WriteConsumerBefore(client.System, Name, invoker, invocation);
                        var invokeResult = await refer.Invoke<T>(invocation);
                        invokeResult.ElapsedMilliseconds = watchInner.ElapsedMilliseconds;
                        _source.WriteConsumerAfter(client.System, Name, invoker, invocation, invokeResult);
                        await pool.Recovery(client);
                        goodUrls.Add(invoker);
                        return new ClusterResult<T>(invokeResult,
                            goodUrls, badUrls,
                            null, false);
                    }
                    catch (Exception ex)
                    {
                        _source.WriteConsumerError(client.System, Name, invoker, invocation, ex, watchInner.ElapsedMilliseconds);
                        await pool.DestoryClient(client).ConfigureAwait(false);
                        throw;
                    }
                }
                catch (Exception e)
                {
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
                    return new ClusterResult<T>(new RpcResult<T>(e) { ElapsedMilliseconds = watchInner.ElapsedMilliseconds },
                        goodUrls, badUrls,
                        e, true);
                }
                finally 
                {
                    watchInner.Stop();
                }
            }

            var exMerger = new Exception($"merger: {merger} is null and the invokers is empty");
            return new ClusterResult<T>(new RpcResult<T>(exMerger), 
                goodUrls, badUrls, 
                exMerger, true);
        }

        Type? returnType = invocation.TargetType.GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes)?.ReturnType;

        object? resultValue = null;
        var watchAll = Stopwatch.StartNew();
        try
        {
            var results = new Dictionary<string, Task<IResult<T>>>();
            foreach (var invoker in invokers)
            {
                var task = Task.Run(async () =>
                {
                    var watchInner = Stopwatch.StartNew();
                    try
                    {
                        var client = await pool.GetClient(invoker);
                        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        try
                        {
                            var refer = await client.Refer();
                            _source.WriteConsumerBefore(client.System, Name, invoker, invocation);
                            var invokeResult = await refer.Invoke<T>(invocation);
                            invokeResult.ElapsedMilliseconds = watchInner.ElapsedMilliseconds;
                            _source.WriteConsumerAfter(client.System, Name, invoker, invocation, invokeResult);
                            await pool.Recovery(client);
                            goodUrls.Add(invoker);
                            return invokeResult;
                        }
                        catch (Exception ex)
                        {
                            await pool.DestoryClient(client);
                            _source.WriteConsumerError(client.System, Name, invoker, invocation, ex, watchInner.ElapsedMilliseconds);
                            throw;
                        }
                    }
                    catch (Exception e)
                    {
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
                        return new RpcResult<T>(e) { ElapsedMilliseconds = watchInner.ElapsedMilliseconds };
                    }
                    finally
                    {
                        watchInner.Stop();
                    }
                });
                results.Add(invoker.ServiceKey??"", task);
            }

            var resultList = new List<IResult<T>>(results.Count);
            int timeout = address.GetMethodParameter(invocation.MethodInfo.Name, TIMEOUT_KEY, DEFAULT_TIMEOUT);

            Task.WaitAll(results.Values.ToArray(), timeout);
            foreach (var entry in results)
            {
                var r = await entry.Value;
                if (r.HasException)
                {
                    _logger.LogError(r.Exception, $"Invoke {entry.Key} {GetGroupDescFromServiceKey(entry.Key)}  failed: {r.Exception?.Message}");
                    return new ClusterResult<T>(new RpcResult<T>(r.Exception) { ElapsedMilliseconds = watchAll.ElapsedMilliseconds }, 
                        goodUrls, badUrls, 
                        r.Exception, true);
                }
                else
                {
                    resultList.Add(r);
                }
            }

            if (resultList.Count == 0)
            {
                return new ClusterResult<T>(new RpcResult<T>() { ElapsedMilliseconds = watchAll.ElapsedMilliseconds }, 
                    goodUrls, badUrls,
                    null, false);
            }
            else if (resultList.Count == 1)
            {
                return new ClusterResult<T>(resultList[0], 
                    goodUrls, badUrls,
                    null, false);
            }

            if (returnType == typeof(void))
            {
                return new ClusterResult<T>(new RpcResult<T>() { ElapsedMilliseconds = watchAll.ElapsedMilliseconds }, 
                    goodUrls, badUrls, 
                    null, false);
            }

            if (merger.StartsWith('.'))
            {
                merger = merger.Substring(1);
                MethodInfo? method;
                try
                {
                    method = returnType!.GetMethod(merger, new[] { returnType });
                }
                catch (Exception e)
                {
                    var ex = new RpcException($"Can not merge result because missing method [{merger}] in class [{returnType!.Name}]{e.Message}", e);
                    return new ClusterResult<T>(new RpcResult<T>(ex), 
                        goodUrls, badUrls,
                        ex, true);
                }

                if (method == null)
                {
                    var ex = new RpcException($"Can not merge result because missing method [ {merger} ] in class [ {returnType.Name} ]");
                    return new ClusterResult<T>(new RpcResult<T>(ex), 
                        goodUrls, badUrls,
                        ex, true);
                }

                //if (!method.IsPublic)
                //{
                //method.setAccessible(true);
                //}
                resultValue = resultList[0].Value;
                resultList.RemoveAt(0);
                try
                {
                    if (method.ReturnType != typeof(void)
                            && method.ReturnType.IsAssignableFrom(resultValue!.GetType()))
                    {
                        foreach (var r in resultList)
                        {
                            resultValue = method.Invoke(resultValue, new object[] { r.Value! });
                        }
                    }
                    else
                    {
                        foreach (var r in resultList)
                        {
                            method.Invoke(resultValue, new object[] { r.Value! });
                        }
                    }
                }
                catch (Exception e)
                {
                    var ex = new RpcException($"Can not merge result: {e.Message}", e);
                    return new ClusterResult<T>(new RpcResult<T>(ex), 
                        goodUrls, badUrls,
                        ex, true);
                }
            }
            else
            {
                IMerger? resultMerger = null;
                if (bool.TrueString.Equals(merger, StringComparison.OrdinalIgnoreCase) 
                    || "default".Equals(merger, StringComparison.OrdinalIgnoreCase))
                {
                    resultMerger = MergerFactory.GetMerger(returnType!, _defaultMergers);
                }
                //else
                //{
                //    resultMerger = _mySelfMergers[merger];
                //}

                if (resultMerger != null)
                {
                    var rets = new List<object>(resultList.Count);
                    foreach (var r in resultList)
                    {
                        rets.Add(r.Value!);
                    }
                    resultValue = resultMerger.GetType().GetMethod("Merge", new Type[] { returnType! })!.Invoke(resultMerger, new[] { rets });
                }
                else
                {
                    var ex = new RpcException("There is no merger to merge result.");
                    return new ClusterResult<T>(new RpcResult<T>(ex), 
                        goodUrls, badUrls, 
                        ex, true);
                }
            }
            return new ClusterResult<T>(new RpcResult<T>((T)resultValue.ChangeType(typeof(T))!) { ElapsedMilliseconds = watchAll.ElapsedMilliseconds }, 
                goodUrls, badUrls,
                null, false);
        }
        catch (Exception ex)
        {
            Debug.Print(ex.StackTrace);
            throw;
        }
        finally 
        {
                watchAll.Stop();
        }
        
    }
}
