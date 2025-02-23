namespace Zooyard.Utils;

/// <summary>
/// 异步转同步,防止ASP.NET中死锁
/// https://cpratt.co/async-tips-tricks/
/// </summary>
internal static class AsyncHelper
{
    //private static readonly TaskFactory _myTaskFactory = new (CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

    /// <summary>
    /// 同步执行
    /// </summary>
    /// <param name="func">任务</param>
    public static void RunSync(Func<Task> func)
    {
        func().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
        //func().ConfigureAwait(false).GetAwaiter().GetResult();
        //_myTaskFactory.StartNew(func).Unwrap().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 同步执行
    /// </summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="func">任务</param>
    /// <returns></returns>
    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        return func().ConfigureAwait(false).GetAwaiter().GetResult();
        //return func().ConfigureAwait(false).GetAwaiter().GetResult();
        //return _myTaskFactory.StartNew(func).Unwrap().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
