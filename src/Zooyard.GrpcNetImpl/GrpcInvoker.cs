using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Zooyard.DataAnnotations;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.GrpcNetImpl;

public class GrpcInvoker(ILogger logger, object _instance, int _clientTimeout) : AbstractInvoker(logger)
{
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
            callOption.WithDeadline(DateTime.UtcNow.AddMilliseconds(_clientTimeout));
        }
        parasPlus[invocation.Arguments.Length] = callOption;

        var methodName = invocation.MethodInfo.Name;
        if (!methodName.EndsWith("Async") 
            && invocation.MethodInfo.ReturnType.IsGenericType 
            && (invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(AsyncUnaryCall<>)
             || invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))) 
        {
            methodName += "Async";
        }
        var method = _instance.GetType().GetMethod(methodName, paraTypes);

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
                var result = new RpcResult<T>();
                return result;
            }

            if (taskResult.GetType().GetTypeInfo().IsGenericType 
                && taskResult.GetType().GetGenericTypeDefinition() == typeof(AsyncUnaryCall<>))
            {
                var resultTask = (AsyncUnaryCall<T>)taskResult;
                var resultData = await resultTask;
                var result = new RpcResult<T>(resultData);
                return result;
            }
            else
            {
                var result = new RpcResult<T>((T)taskResult.ChangeType(typeof(T))!);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }
}
