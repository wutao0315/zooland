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

            if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task))
            {
                await (dynamic)taskInvoke;
                return new RpcResult();
            }
            var valueOut = await (dynamic)taskInvoke;
            return new RpcResult(valueOut);

        }
    }
}
