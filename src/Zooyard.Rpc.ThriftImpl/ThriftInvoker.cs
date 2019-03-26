using Microsoft.Extensions.Logging;
using System;
using Zooyard.Core;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftInvoker : IInvoker
    {
        private readonly IDisposable _instance;
        private readonly ILogger _logger;
        public ThriftInvoker(IDisposable instance,ILoggerFactory loggerFactory)
        {
            _instance = instance;
            _logger = loggerFactory.CreateLogger<ThriftInvoker>();
        }

        public object Instance { get { return _instance; } }
        public IResult Invoke(IInvocation invocation)
        {
            //var methodName = $"{invocation.MethodInfo.Name}Async";
            //var argumentTypes = new List<Type>(invocation.ArgumentTypes);
            //argumentTypes.Add(typeof(CancellationToken));
            //var arguments = new List<object>(invocation.Arguments);
            //arguments.Add(CancellationToken.None);

            //var method = Instance.GetType().GetMethod(methodName, argumentTypes.ToArray());
            //var value = method.Invoke(Instance, arguments.ToArray());
            //return new RpcResult(value);
            
            var method = _instance.GetType().GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            var value = method.Invoke(_instance, invocation.Arguments);
            _logger.LogInformation($"Invoke:{invocation.MethodInfo.Name}");
            return new RpcResult(value);
        }
    }
}
