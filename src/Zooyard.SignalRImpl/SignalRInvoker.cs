using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Zooyard.Rpc.Support;

namespace Zooyard.SignalRImpl;

public class SignalRInvoker(ILogger logger, HubConnection _instance, int _clientTimeout, URL _url) : AbstractInvoker(logger)
{
    public override object Instance =>_instance;
    public override int ClientTimeout => _clientTimeout;
    protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
    {
        //执行超时逻辑
        using var cts = new CancellationTokenSource(_clientTimeout);

        var value = await TimeoutAsync(_instance.InvokeCoreAsync<T>(invocation.MethodInfo.Name, invocation.Arguments), _clientTimeout, cts, $"{invocation.Url} {invocation.TargetType.FullName} {invocation.MethodInfo.Name} {invocation.Arguments}");

        if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task)) 
        {
            return new RpcResult<T>();
        }

        return new RpcResult<T>(value);

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
