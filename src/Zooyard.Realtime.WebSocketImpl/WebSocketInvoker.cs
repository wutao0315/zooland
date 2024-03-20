using Microsoft.Extensions.Logging;
using Zooyard.Realtime.Connection;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.WebSocketsImpl;

public class WebSocketInvoker(ILogger logger, RpcConnection _instance, int _clientTimeout, URL _url) : AbstractInvoker(logger)
{
    public override object Instance => _instance;
    public override int ClientTimeout => _clientTimeout;
    protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
    {
        if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task))
        {
            //执行超时逻辑
            using var cts = new CancellationTokenSource(_clientTimeout);
            await TimeoutVoidAsync(_instance.InvokeCoreAsync(invocation.MethodInfo.Name, invocation.Arguments, RpcContext.GetContext().Attachments, cts.Token), _clientTimeout, cts, $"{invocation.Url} {invocation.TargetType.FullName} {invocation.MethodInfo.Name} {invocation.Arguments}");

            return new RpcResult<T>();
        }
        else 
        {
            //执行超时逻辑
            using var cts = new CancellationTokenSource(_clientTimeout);
            var value = await TimeoutAsync(_instance.InvokeCoreAsync<T>(invocation.MethodInfo.Name, invocation.Arguments, RpcContext.GetContext().Attachments, cts.Token), _clientTimeout, cts, $"{invocation.Url} {invocation.TargetType.FullName} {invocation.MethodInfo.Name} {invocation.Arguments}");

            if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task))
            {
                return new RpcResult<T>();
            }

            return new RpcResult<T>(value);
        }
        async Task TimeoutVoidAsync(Task task, int millisecondsDelay, CancellationTokenSource cts, string message)
        {
            try
            {
                if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)) == task)
                {
                    if (task.Exception != null)
                        throw task.Exception;

                    return;
                }

                cts.Cancel();

                throw new TimeoutException($"time out {millisecondsDelay} when invoke {message}");
            }
            catch
            {
                throw;
            }
        }
        async Task<TT> TimeoutAsync<TT>(Task<TT> task, int millisecondsDelay, CancellationTokenSource cts, string message)
        {
            try
            {
                if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)) == task)
                {
                    if (task.Exception != null)
                        throw task.Exception;

                    return task.Result;
                }

                cts.Cancel();

                throw new TimeoutException($"time out {millisecondsDelay} when invoke {message}");
            }
            catch
            {
                throw;
            }
        }
    }
}
