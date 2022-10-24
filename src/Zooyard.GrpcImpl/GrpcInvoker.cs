using Grpc.Core;
using System.Diagnostics;
using System.Net.Mime;
using System.Reflection;
using Zooyard.DataAnnotations;
using Zooyard.Logging;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.GrpcImpl;

public class GrpcInvoker : AbstractInvoker
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(GrpcInvoker));

    private readonly object _instance;
    private readonly int _clientTimeout;

    public GrpcInvoker(object instance, int clientTimeout)
    {
        _instance = instance;
        _clientTimeout = clientTimeout;
    }
    public override object Instance => _instance;
    public override int ClientTimeout => _clientTimeout;
    protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
    {
        var paraTypes = new Type[invocation.Arguments.Length + 1];
        var parasPlus = new object[invocation.Arguments.Length + 1];
        for (var i = 0; i < invocation.Arguments.Length; i++)
        {
            paraTypes[i] = invocation.Arguments[i].GetType();
            parasPlus[i] = invocation.Arguments[i];
        }
        paraTypes[invocation.Arguments.Length] = typeof(CallOptions);

        var callOption = new CallOptions();
        if (_clientTimeout > 0)
        {
            callOption.WithDeadline(DateTime.Now.AddMilliseconds(_clientTimeout));
        }
        parasPlus[invocation.Arguments.Length] = callOption;

        var method = _instance.GetType().GetMethod(invocation.MethodInfo.Name, paraTypes);

        var watch = Stopwatch.StartNew();
        try
        {
            if (method == null) 
            {
                throw new Exception($"method {invocation.MethodInfo.Name} not exits");
            }

            var header = new Dictionary<string, string>();
            var targetDescription = invocation.TargetType.GetCustomAttribute<RequestMappingAttribute>();
            if (targetDescription != null)
            {
                header = targetDescription.Headers;
            }
            var methodDescription = invocation.MethodInfo.GetCustomAttribute<RequestMappingAttribute>();
            if (methodDescription != null)
            {
                foreach (var item in methodDescription.Headers)
                {
                    header[item.Key] = item.Value;
                }
            }

            foreach (var item in header)
            {
                RpcContext.GetContext().SetAttachment(item.Key, item.Value);
            }

            var taskResult = method.Invoke(_instance, parasPlus);
            if (taskResult == null) 
            {
                var result = new RpcResult<T>(default!, watch.ElapsedMilliseconds);
                return result;
            }

            if (taskResult.GetType().GetTypeInfo().IsGenericType 
                && taskResult.GetType().GetGenericTypeDefinition() == typeof(AsyncUnaryCall<>))
            {
                var resultData = await (AsyncUnaryCall<T>)taskResult;
                watch.Stop();
                var result = new RpcResult<T>(resultData, watch.ElapsedMilliseconds);
                return result;
            }
            else
            {
                watch.Stop();

                var result = new RpcResult<T>((T)taskResult.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
                return result;
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
            {
                watch.Stop();
            }
            Logger().LogInformation($"Grpc Invoke {watch.ElapsedMilliseconds} ms");
        }
    }
}
