using Grpc.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Zooyard;
using Zooyard.Logging;
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
                var taskResult = method.Invoke(_instance, parasPlus);
                if (taskResult.GetType().GetTypeInfo().IsGenericType &&
                          taskResult.GetType().GetGenericTypeDefinition() == typeof(AsyncUnaryCall<>))
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
                throw ex;
            }
            finally
            {
                if (watch.IsRunning)
                    watch.Stop();
                Logger().LogInformation($"Grpc Invoke {watch.ElapsedMilliseconds} ms");
            }
            
        }
    }
}
