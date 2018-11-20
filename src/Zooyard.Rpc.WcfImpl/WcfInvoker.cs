using Microsoft.Extensions.Logging;
using Zooyard.Core;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfInvoker : IInvoker
    {
        private readonly object _instance;
        private readonly ILogger _logger;
        public WcfInvoker(object instance,ILoggerFactory loggerFactory)
        {
            _instance = instance;
            _logger = loggerFactory.CreateLogger<WcfInvoker>();
        }

        public IResult Invoke(IInvocation invocation)
        {
            var method = _instance.GetType().GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            var value = method.Invoke(_instance, invocation.Arguments);
            _logger.LogInformation($"Invoke:{invocation.MethodInfo.Name}");
            return new RpcResult(value);
        }
    }
}
