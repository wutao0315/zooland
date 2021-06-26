using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftInvoker : AbstractInvoker
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ThriftInvoker));
        private readonly TBaseClient _instance;
        private readonly int _clientTimeout;

        public ThriftInvoker(TBaseClient instance, int clientTimeout)
        {
            _instance = instance;
            _clientTimeout = clientTimeout;
        }

        public override object Instance => _instance;

        public override int ClientTimeout => _clientTimeout;

        protected override async Task<IResult> HandleInvoke(IInvocation invocation)
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
            if (!invocation.MethodInfo.Name.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
            {
                methodName += "Async";
            }


            var method = _instance.GetType().GetMethod(methodName, argumentTypes.ToArray());
            var taskInvoke = method.Invoke(_instance, arguments.ToArray());
            //var result = await (dynamic)taskInvoke;

            //if (invocation.MethodInfo.ReturnType == typeof(Task) ||
            //  (invocation.MethodInfo.ReturnType.IsGenericType &&
            //   invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
            //{
            //    return new RpcResult(Task.FromResult(result));
            //}
            //return new RpcResult(result);

            if (invocation.MethodInfo.ReturnType == typeof(Task))
            {
                await (dynamic)taskInvoke;
                return new RpcResult(Task.CompletedTask);
            }
            else if (invocation.MethodInfo.ReturnType.IsGenericType && invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var invokeValue = await (dynamic)taskInvoke;
                return new RpcResult(Task.FromResult(invokeValue));
            }

            var awaiter = taskInvoke.GetType().GetMethod("GetAwaiter").Invoke(taskInvoke, new object[] { });
            var value = awaiter.GetType().GetMethod("GetResult").Invoke(awaiter, new object[] { });

            return new RpcResult(value);

            //object value = null;
            //var task = taskInvoke as Task<object>;
            //if (task != null)
            //{
            //    value = task.GetAwaiter().GetResult();
            //}

            //var method = _instance.GetType().GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            //var value = method.Invoke(_instance, invocation.Arguments);
            //_logger.LogInformation($"Invoke:{invocation.MethodInfo.Name}");
            //return new RpcResult(value);
        }
    }
}
