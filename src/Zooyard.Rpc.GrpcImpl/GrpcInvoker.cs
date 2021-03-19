using System;
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
            paraTypes[invocation.Arguments.Length] = typeof(Grpc.Core.CallOptions);

            var callOption = new Grpc.Core.CallOptions();
            if (_clientTimeout>0) 
            {
                callOption.WithDeadline(DateTime.UtcNow.AddMilliseconds(_clientTimeout));
            }
            parasPlus[invocation.Arguments.Length] = callOption;
                
            var method = _instance.GetType().GetMethod(invocation.MethodInfo.Name, paraTypes);
            var value = method.Invoke(_instance, parasPlus);
            await Task.CompletedTask;
            Logger().LogInformation($"Invoke:{invocation.MethodInfo.Name}");
            var result = new RpcResult(value);
            return result;
        }
    }
}
