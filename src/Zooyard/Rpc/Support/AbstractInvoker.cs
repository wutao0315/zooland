﻿//using Zooyard.Logging;

using Microsoft.Extensions.Logging;

namespace Zooyard.Rpc.Support;

public abstract class AbstractInvoker : IInvoker
{
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(AbstractClientPool));
    protected readonly ILogger _logger;
    public AbstractInvoker(ILogger logger) 
    {
        _logger = logger;
    }
    public abstract int ClientTimeout { get; }
    public abstract object Instance { get; }
    public virtual async Task<IResult<T>> Invoke<T>(IInvocation invocation)
    {
        var message = $"{invocation.ServiceName}:{invocation.Version}:{invocation.TargetType.FullName}:{invocation.MethodInfo.Name}";
#if DEBUG
        var result = await HandleInvoke<T>(invocation);
#else
        using var cts = new CancellationTokenSource(ClientTimeout);
        var result = await Timeout(HandleInvoke<T>(invocation), ClientTimeout, cts, message);

        static async Task<TT> Timeout<TT>(Task<TT> task, int millisecondsDelay, CancellationTokenSource cts, string message)
        {
            if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)).ConfigureAwait(false) == task)
                return task.Result;

            cts.Cancel();

            throw new TimeoutException($"time out {millisecondsDelay} when invoke {message}");
        }
#endif
        _logger.LogInformation(message);
        return result;
    }
    protected abstract Task<IResult<T>> HandleInvoke<T>(IInvocation invocation);

    
}
