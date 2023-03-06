using System.Diagnostics;
using System.Text.Json;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.DotNettyImpl.Transport.Codec;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.DotNettyImpl;

public class NettyInvoker : AbstractInvoker
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyInvoker));

    private readonly ITransportClient _channel;
    private readonly int _clientTimeout;

    public NettyInvoker(ITransportClient channel, int clientTimeout)
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
            Arguments = invocation.Arguments,
            ArgumentTypes = invocation.ArgumentTypes,
            //Arguments = { from a in invocation.Arguments select Any.Pack((IMessage)a) }
        };

        var watch = Stopwatch.StartNew();
        
        try
        {
            var response = await _channel.SendAsync(message, CancellationToken.None);
            watch.Stop();
            if (invocation.MethodInfo.ReturnType == typeof(Task))
            {
                return new RpcResult<T>(watch.ElapsedMilliseconds);
            }
            else if (invocation.MethodInfo.ReturnType.IsGenericType 
                && invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                if (typeof(T).IsValueType || typeof(T) == typeof(string))
                {
                    return new RpcResult<T>((T)response.Data.ChangeType(typeof(T))!, watch.ElapsedMilliseconds);
                }
                else 
                {
                    var data = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(response.Data, JsonTransportMessageCodecFactory._option), JsonTransportMessageCodecFactory._option)!;

                    return new RpcResult<T>(data, watch.ElapsedMilliseconds);
                }
            }

            if (typeof(T).IsValueType || typeof(T) == typeof(string))
            {
                return new RpcResult<T>((T)response.Data!, watch.ElapsedMilliseconds);
            }
            else
            {
                var data = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(response.Data, JsonTransportMessageCodecFactory._option), JsonTransportMessageCodecFactory._option)!;

                return new RpcResult<T>(data, watch.ElapsedMilliseconds);
            }
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
            Logger().LogInformation($"Netty Invoke {watch.ElapsedMilliseconds} ms");
        }
    }
}
