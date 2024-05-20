using Microsoft.Extensions.Logging;
#if !DEBUG
using Zooyard.Utils;
#endif

namespace Zooyard.Rpc.Support;

public abstract class AbstractInvoker : IInvoker
{
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
        var result = await TaskUtil.Timeout(HandleInvoke<T>(invocation), ClientTimeout, cts, $"time out {ClientTimeout} when invoke {message}");
#endif
        _logger.LogInformation(message);
        return result;
    }
    protected abstract Task<IResult<T>> HandleInvoke<T>(IInvocation invocation);

    
}
