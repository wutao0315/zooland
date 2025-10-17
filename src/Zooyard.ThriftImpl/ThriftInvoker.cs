using Microsoft.Extensions.Logging;
using Thrift;
using Zooyard.Rpc.Support;

namespace Zooyard.ThriftImpl;

public class ThriftInvoker(ILogger logger, TBaseClient _instance, int _clientTimeout) : AbstractInvoker(logger)
{
    public override object Instance => _instance;

    public override int ClientTimeout => _clientTimeout;

    protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
    {
        var argumentTypes = new List<Type>(invocation.ArgumentTypes) 
        {
            typeof(CancellationToken)
        };
        var arguments = new List<object>(invocation.Arguments)
        {
            CancellationToken.None
        };

        var methodName = invocation.MethodInfo.Name;

        //if (!invocation.MethodInfo.Name.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
        //{
        //    methodName += "Async";
        //}

        var method = _instance.GetType().GetMethod(methodName, argumentTypes.ToArray());

        if (method == null) 
        {
            throw new Exception($"{_instance.GetType().FullName} not contians method {methodName} {argumentTypes}");
        }

        try
        {
            var taskInvoke = method.Invoke(_instance, arguments.ToArray())!;
            if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task))
            {
                await (Task)taskInvoke;
                return new RpcResult<T>();
            }
            var valueOut = await (Task<T>)taskInvoke;
            return new RpcResult<T>(valueOut);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }
}
