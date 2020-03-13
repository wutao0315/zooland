using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractInvoker : IInvoker
    {
        private readonly ILogger _logger;
        public AbstractInvoker(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AbstractInvoker>();
        }
        public abstract object Instance { get; }
        public virtual async Task<IResult> Invoke(IInvocation invocation)
        {
            _logger.LogInformation($"{invocation.App}:{invocation.Version}:{invocation.TargetType.FullName}:{invocation.MethodInfo.Name}");
            var result = await HandleInvoke(invocation);
            return result;
        }
        protected abstract Task<IResult> HandleInvoke(IInvocation invocation);
    }
}
