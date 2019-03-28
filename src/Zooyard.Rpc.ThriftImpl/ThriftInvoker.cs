using Microsoft.Extensions.Logging;
using System;
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
        protected override IResult HandleInvoke(IInvocation invocation)
        {
            var method = _instance.GetType().GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            var value = method.Invoke(_instance, invocation.Arguments);
            _logger.LogInformation($"Invoke:{invocation.MethodInfo.Name}");
            return new RpcResult(value);
        }
    }
}
