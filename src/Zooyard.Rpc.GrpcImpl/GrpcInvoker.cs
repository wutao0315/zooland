using Grpc.Core;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcInvoker : AbstractInvoker
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(GrpcInvoker));

        private readonly object _instance;
        private readonly int _clientTimeout;

        public GrpcInvoker(object instance, int clientTimeout)
        {
            _instance = instance;
            _clientTimeout = clientTimeout;
        }
        public override object Instance => _instance;
        public override int ClientTimeout => _clientTimeout;
        protected override async Task<IResult> HandleInvoke(IInvocation invocation)
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
            Logger().LogInformation($"Invoke:{invocation.MethodInfo.Name}");

            var taskResult = method.Invoke(_instance, parasPlus);

            
            if (taskResult.GetType().GetTypeInfo().IsGenericType &&
                          taskResult.GetType().GetGenericTypeDefinition() == typeof(AsyncUnaryCall<>))
            {
                var resultData = await (dynamic)taskResult;
                //var awaiter = taskResult.GetType().GetMethod("GetAwaiter").Invoke(taskResult, new object[] { });
                //var resultData = awaiter.GetType().GetMethod("GetResult").Invoke(awaiter, new object[] { });
                var resultValue = Task.FromResult(resultData);
                var result = new RpcResult(resultValue);
                return result;
            }
            else 
            {
                var result = new RpcResult(taskResult);
                return result;
            }

            //dynamic taskInvoke = method.Invoke(_instance, parasPlus);
            //dynamic result = await taskInvoke;
            //return new RpcResult(result);

            //var awaiter = taskInvoke.GetType().GetMethod("GetAwaiter").Invoke(taskInvoke, new object[] { });
            //var value = awaiter.GetType().GetMethod("GetResult").Invoke(awaiter, new object[] { });
            //var result = Task.FromResult(value);

            //var result = new RpcResult(value);
            //return result;
        }
    }
}
