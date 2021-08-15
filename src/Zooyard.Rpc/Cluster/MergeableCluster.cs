using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Zooyard;
using Zooyard.Diagnositcs;
using Zooyard.Logging;
using Zooyard.Rpc.Merger;

namespace Zooyard.Rpc.Cluster
{
    public class MergeableCluster : AbstractCluster
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(MergeableCluster));
        public override string Name => NAME;
        public const string NAME = "mergeable";
        public const string MERGER_KEY = "merger";

        private readonly IDictionary<Type, IMerger> _defaultMergers;
        private readonly IDictionary<string, IMerger> _mySelfMergers;
        public MergeableCluster(IDictionary<Type, IMerger> defaultMergers,
            IDictionary<string, IMerger> mySelfMergers)
        {
            _defaultMergers = defaultMergers;
            _mySelfMergers = mySelfMergers;
        }

        private string getGroupDescFromServiceKey(string key)
        {
            int index = key.IndexOf("/");
            if (index > 0)
            {
                return $"group [ {key.Substring(0, index)} ]";
            }
            return key;
        }
        public override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> urls, IInvocation invocation)
        {
            var goodUrls = new List<URL>();
            var badUrls = new List<BadUrl>();
            var invokers = urls;

            var merger = address.GetMethodParameter(invocation.MethodInfo.Name, MERGER_KEY);
            // If a method doesn't have a merger, only invoke one Group
            if (string.IsNullOrEmpty(merger))
            {
                foreach (var invoker in invokers)
                {
                    try
                    {
                        var client =await pool.GetClient(invoker);
                        try
                        {
                            var refer = await client.Refer();
                            _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                            var invokeResult = await refer.Invoke<T>(invocation);
                            _source.WriteConsumerAfter(invoker, invocation, invokeResult);
                            await pool.Recovery(client);
                            goodUrls.Add(invoker);
                            return new ClusterResult<T>(invokeResult, goodUrls, badUrls, null, false);
                        }
                        catch (Exception ex)
                        {
                            _source.WriteConsumerError(invoker,invocation ,ex);
                            await pool.DestoryClient(client).ConfigureAwait(false);
                            throw ex;
                        }
                    }
                    catch (Exception e)
                    {
                        badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = e });
                        return new ClusterResult<T>(new RpcResult<T>(e), goodUrls, badUrls, e, true);
                    }
                }

                var exMerger = new Exception($"merger: {merger} is null and the invokers is empty");
                return new ClusterResult<T>(new RpcResult<T>(exMerger), goodUrls, badUrls, exMerger, true);
            }

            Type returnType = invocation.TargetType.GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes).ReturnType;

            object resultValue = null;
            var watch = Stopwatch.StartNew();
            try
            {
                var results = new Dictionary<string, Task<IResult<T>>>();
                foreach (var invoker in invokers)
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var client = await pool.GetClient(invoker);
                            try
                            {
                                var refer = await client.Refer();
                                _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                                var invokeResult = await refer.Invoke<T>(invocation);
                                _source.WriteConsumerAfter(invoker, invocation, invokeResult);
                                await pool.Recovery(client);
                                goodUrls.Add(invoker);
                                return invokeResult;
                            }
                            catch (Exception ex)
                            {
                                await pool.DestoryClient(client);
                                _source.WriteConsumerError(invoker, invocation, ex);
                                throw ex;
                            }
                        }
                        catch (Exception e)
                        {
                            badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = e });
                            return new RpcResult<T>(e);
                        }
                    });
                    results.Add(invoker.ServiceKey, task);
                }

                var resultList = new List<IResult<T>>(results.Count);
                int timeout = address.GetMethodParameter(invocation.MethodInfo.Name, TIMEOUT_KEY, DEFAULT_TIMEOUT);

                Task.WaitAll(results.Values.ToArray(), timeout);
                foreach (var entry in results)
                {
                    var r = await entry.Value;
                    if (r.HasException)
                    {
                        Logger().LogError(r.Exception, $"Invoke {entry.Key} {getGroupDescFromServiceKey(entry.Key)}  failed: {r.Exception.Message}");
                        return new ClusterResult<T>(new RpcResult<T>(r.Exception), goodUrls, badUrls, r.Exception, true);
                    }
                    else
                    {
                        resultList.Add(r);
                    }
                }

                watch.Stop();

                if (resultList.Count == 0)
                {
                    return new ClusterResult<T>(new RpcResult<T>(watch.ElapsedMilliseconds), goodUrls, badUrls, null, false);
                }
                else if (resultList.Count == 1)
                {
                    return new ClusterResult<T>(resultList[0], goodUrls, badUrls, null, false);
                }

                if (returnType == typeof(void))
                {
                    return new ClusterResult<T>(new RpcResult<T>(watch.ElapsedMilliseconds), goodUrls, badUrls, null, false);
                }

                if (merger.StartsWith("."))
                {
                    merger = merger.Substring(1);
                    MethodInfo method;
                    try
                    {
                        method = returnType.GetMethod(merger, new[] { returnType });
                    }
                    catch (Exception e)
                    {
                        var ex = new RpcException($"Can not merge result because missing method [{merger}] in class [{returnType.Name}]{e.Message}", e);
                        return new ClusterResult<T>(new RpcResult<T>(ex), goodUrls, badUrls, ex, true);
                    }

                    if (method == null)
                    {
                        var ex = new RpcException($"Can not merge result because missing method [ {merger} ] in class [ {returnType.Name} ]");
                        return new ClusterResult<T>(new RpcResult<T>(ex), goodUrls, badUrls, ex, true);
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
                                && method.ReturnType.IsAssignableFrom(resultValue.GetType()))
                        {
                            foreach (var r in resultList)
                            {
                                resultValue = method.Invoke(resultValue, new object[] { r.Value });
                            }
                        }
                        else
                        {
                            foreach (var r in resultList)
                            {
                                method.Invoke(resultValue, new object[] { r.Value });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var ex = new RpcException($"Can not merge result: {e.Message}", e);
                        return new ClusterResult<T>(new RpcResult<T>(ex), goodUrls, badUrls, ex, true);
                    }
                }
                else
                {
                    IMerger resultMerger;
                    if (merger.ToLower() == "true" || merger.ToLower() == "default")
                    {
                        resultMerger = MergerFactory.GetMerger(returnType, _defaultMergers);
                    }
                    else
                    {
                        resultMerger = _mySelfMergers[merger];
                    }

                    if (resultMerger != null)
                    {
                        var rets = new List<object>(resultList.Count);
                        foreach (var r in resultList)
                        {
                            rets.Add(r.Value);
                        }
                        resultValue = resultMerger.GetType().GetMethod("Merge", new Type[] { returnType }).Invoke(resultMerger, new[] { rets });
                    }
                    else
                    {
                        var ex = new RpcException("There is no merger to merge result.");
                        return new ClusterResult<T>(new RpcResult<T>(ex), goodUrls, badUrls, ex, true);
                    }
                }
                return new ClusterResult<T>(new RpcResult<T>((T)resultValue.ChangeType(typeof(T)), watch.ElapsedMilliseconds), goodUrls, badUrls, null, false);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.StackTrace);
                throw ex;
            }
            finally 
            {
                if (watch.IsRunning)
                    watch.Stop();
            }
            
        }
    }
}
