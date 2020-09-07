using System;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractInvoker : IInvoker
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(AbstractClientPool));
       
        public abstract object Instance { get; }
        public virtual async Task<IResult> Invoke(IInvocation invocation)
        {
            Logger().Information($"{invocation.App}:{invocation.Version}:{invocation.TargetType.FullName}:{invocation.MethodInfo.Name}");
            var result = await HandleInvoke(invocation);
            return result;
        }
        protected abstract Task<IResult> HandleInvoke(IInvocation invocation);
    }
}
