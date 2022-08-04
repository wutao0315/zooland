using DotNetty.Transport.Channels;
using System.Diagnostics;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.NettyImpl.Support;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.NettyImpl;

public class DotNettyInvoker : AbstractInvoker
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyInvoker));

    private readonly IChannel _channel;
    private readonly int _clientTimeout;

    public DotNettyInvoker(IChannel channel, int clientTimeout)
    {
        _channel = channel;
        _clientTimeout = clientTimeout;
    }
    public override object Instance => _channel;
    public override int ClientTimeout => _clientTimeout;
    protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
    {
        var message = new RemoteInvokeMessage
        {
            Method = invocation.MethodInfo.Name,
            Arguments = invocation.Arguments
        };

        //注册结果回调

        var watch = Stopwatch.StartNew();
        try
        {
            var rpcMsg = NettyRemotingClient.Instance.BuildRequestMessage(message, ProtocolConstants.MSGTYPE_RESQUEST_SYNC);
            var result = await NettyRemotingClient.Instance.SendSync(_channel, rpcMsg, _clientTimeout);
            watch.Stop();

            if (invocation.MethodInfo.ReturnType == typeof(Task))
            {
                return new RpcResult<T>(watch.ElapsedMilliseconds);
            }
            else if (invocation.MethodInfo.ReturnType.IsGenericType && invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return new RpcResult<T>((T)result.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
            }

            return new RpcResult<T>((T)result.ChangeType(typeof(T)), watch.ElapsedMilliseconds);

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
