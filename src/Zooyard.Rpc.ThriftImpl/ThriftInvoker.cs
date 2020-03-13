using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftInvoker : AbstractInvoker
    {
        private readonly IDisposable _instance;
        private readonly ILogger _logger;
        public ThriftInvoker(IDisposable instance,ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _instance = instance;
            _logger = loggerFactory.CreateLogger<ThriftInvoker>();
        }

        public override object Instance { get { return _instance; } }
        protected override async Task<IResult> HandleInvoke(IInvocation invocation)
        {
            var methodName = $"{invocation.MethodInfo.Name}Async";
            var argumentTypes = new List<Type>(invocation.ArgumentTypes);
            argumentTypes.Add(typeof(CancellationToken));
            var arguments = new List<object>(invocation.Arguments);
            arguments.Add(CancellationToken.None);

            var method = Instance.GetType().GetMethod(methodName, argumentTypes.ToArray());

            var taskInvoke = method.Invoke(Instance, arguments.ToArray());
            
            var awaiter = taskInvoke.GetType().GetMethod("GetAwaiter").Invoke(taskInvoke,new object[] { });
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
