using Zooyard.Atomic;
using Zooyard.Diagnositcs;
using Zooyard.Logging;

namespace Zooyard.Rpc.Cluster;

/// <summary>
/// 并行调用多个服务，只要一个成功即返回，但是这要消耗更多的资源。
/// </summary>
public class ForkingCluster : AbstractCluster
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ForkingCluster));
    //public ForkingCluster(IEnumerable<ICache> caches) : base(caches) { }
    public override string Name => NAME;
    public const string NAME = "forking";
    public const string FORKS_KEY = "forks";
    public const int DEFAULT_FORKS = 2;

    protected override async Task<IClusterResult<T>> DoInvoke<T>(IClientPool pool, ILoadBalance loadbalance, URL address, IList<URL> invokers, IInvocation invocation)
    {
        //IResult result = null;
        var goodUrls = new List<URL>();
        var badUrls = new List<BadUrl>();

        CheckInvokers(invokers, invocation, address);

        IList<URL> selected;

        int forks = address.GetParameter(FORKS_KEY, DEFAULT_FORKS);
        int timeout = address.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);

        if (forks <= 0 || forks >= invokers.Count)
        {
            selected = invokers;
        }
        else
        {
            selected = new List<URL>();
            for (int i = 0; i < forks; i++)
            {
                //在invoker列表(排除selected)后,如果没有选够,则存在重复循环问题.见select实现.
                var invoker = base.Select(loadbalance, invocation, invokers, selected);
                if (!selected.Contains(invoker))
                {//防止重复添加invoker
                    selected.Add(invoker);
                }
            }
        }
        RpcContext.GetContext().SetInvokers(selected);
        var count = new AtomicInteger();

        var taskList = new Task<IResult<T>?>[selected.Count];
        var index = 0;
        foreach (var invoker in selected)
        {
            var task = Task.Run(async() => {
                try
                {
                    var client = await pool.GetClient(invoker);
                    try
                    {
                        var refer = await client.Refer();
                        _source.WriteConsumerBefore(refer.Instance, invoker, invocation);
                        var resultInner = await refer.Invoke<T>(invocation);
                        _source.WriteConsumerAfter(invoker, invocation, resultInner);
                        await pool.Recovery(client);
                        goodUrls.Add(invoker);
                        return resultInner;
                    }
                    catch (Exception ex)
                    {
                        await pool.DestoryClient(client).ConfigureAwait(false);
                        _source.WriteConsumerError(invoker, invocation, ex);
                        throw;
                    }
                }
                catch (Exception e)
                {
                    badUrls.Add(new BadUrl { Url = invoker, BadTime = DateTime.Now, CurrentException = e });
                    int value = count.IncrementAndGet();
                    if (value >= selected.Count)
                    {
                        return new RpcResult<T>(e);
                    }
                }
                return null;
            });
            taskList[index++] = task;
        }
        try
        {
            var retIndex=Task.WaitAny(taskList, timeout);
            var ret= await taskList[retIndex];

            if (ret == null || ret.HasException)
            {
                Exception? e = ret?.Exception;
                throw new RpcException(e is RpcException exception ? exception.Code : 0, "Failed to forking invoke provider " + selected + ", but no luck to perform the invocation. Last error is: " + e?.Message, e?.InnerException != null ? e.InnerException : e);
            }
            return new ClusterResult<T>(ret, goodUrls, badUrls, null,false);
        }
        catch (Exception e)
        {
            var exception = new RpcException("Failed to forking invoke provider " + selected + ", but no luck to perform the invocation. Last error is: " + e.Message, e);
            return new ClusterResult<T>(new RpcResult<T>(exception), goodUrls, badUrls, exception, true);
        }
    }
}
