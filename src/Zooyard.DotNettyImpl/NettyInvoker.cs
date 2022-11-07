using System.Diagnostics;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.DotNettyImpl;

public class NettyInvoker : AbstractInvoker
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyInvoker));

    private readonly ITransportClient _transportClient;
    private readonly int _clientTimeout;

    public NettyInvoker(ITransportClient transportClient, int clientTimeout)
    {
        _transportClient = transportClient;
        _clientTimeout = clientTimeout;
    }
    public override object Instance => _transportClient;
    public override int ClientTimeout => _clientTimeout;

    protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
    {
        var message = new RemoteInvokeMessage
        {
            Method = invocation.MethodInfo.Name,
            Arguments = invocation.Arguments
        };

        var watch = Stopwatch.StartNew();
        
        try
        {
            var result = await _transportClient.SendAsync(message, CancellationToken.None);
            watch.Stop();
            if (invocation.MethodInfo.ReturnType == typeof(Task))
            {
                return new RpcResult<T>(watch.ElapsedMilliseconds);
                //return new RpcResult(Task.CompletedTask);
            }
            else if (invocation.MethodInfo.ReturnType.IsGenericType && invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return new RpcResult<T>((T)result.Result.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
                //var resultData = Task.FromResult((dynamic)value.Result);
                //return new RpcResult<T>((T)value.Result.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
            }

            return new RpcResult<T>((T)result.Result.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
            //return new RpcResult<T>((T)value.Result.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Debug.Print(ex.StackTrace);
            throw;
        }
        finally
        {
            if (watch.IsRunning)
                watch.Stop();
            Logger().LogInformation($"Thrift Invoke {watch.ElapsedMilliseconds} ms");
        }
    }
}
