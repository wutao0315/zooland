using Microsoft.Extensions.Logging;
using System;
using Zooyard.Core;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcInvoker : IInvoker
    {
        private readonly object _instance;
        private readonly int _clientTimeout;
        private readonly ILogger _logger;
        public GrpcInvoker(object instance,int clientTimeout,ILoggerFactory loggerFactory)
        {
            _instance = instance;
            _clientTimeout = clientTimeout;
            _logger = loggerFactory.CreateLogger<GrpcInvoker>();
        }
        
        public IResult Invoke(IInvocation invocation)
        {
            var paraTypes = new Type[invocation.Arguments.Length + 1];
            var parasPlus = new object[invocation.Arguments.Length + 1];
            for (var i = 0; i < invocation.Arguments.Length; i++)
            {
                paraTypes[i] = invocation.Arguments[i].GetType();
                parasPlus[i] = invocation.Arguments[i];
            }
            paraTypes[invocation.Arguments.Length] = typeof(Grpc.Core.CallOptions);
            parasPlus[invocation.Arguments.Length] = new Grpc.Core.CallOptions()
                .WithDeadline(DateTime.UtcNow.AddMilliseconds(_clientTimeout));
            var method = _instance.GetType().GetMethod(invocation.MethodInfo.Name, paraTypes);
            var value = method.Invoke(_instance, parasPlus);
            _logger.LogInformation($"Invoke:{invocation.MethodInfo.Name}");
            var result = new RpcResult(value);
            return result;
        }
    }
}
