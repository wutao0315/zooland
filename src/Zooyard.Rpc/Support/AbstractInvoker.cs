﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Core.Logging;

namespace Zooyard.Rpc.Support
{
    public abstract class AbstractInvoker : IInvoker
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(AbstractClientPool));
        public abstract int ClientTimeout { get; }
        public abstract object Instance { get; }
        public virtual async Task<IResult> Invoke(IInvocation invocation)
        {
            var message = $"{invocation.App}:{invocation.Version}:{invocation.TargetType.FullName}:{invocation.MethodInfo.Name}";
#if DEBUG
            var result = await HandleInvoke(invocation);
#else
            using var cts = new CancellationTokenSource(ClientTimeout);
            var result = await Timeout(HandleInvoke(invocation), ClientTimeout, cts, message);

            static async Task<T> Timeout<T>(Task<T> task, int millisecondsDelay, CancellationTokenSource cts, string message)
            {
                if (await Task.WhenAny(task, Task.Delay(millisecondsDelay, cts.Token)).ConfigureAwait(false) == task)
                    return task.Result;

                cts.Cancel();

                throw new TimeoutException($"time out {millisecondsDelay} when invoke {message}");
            }
#endif
            Logger().LogInformation(message);
            return result;
        }
        protected abstract Task<IResult> HandleInvoke(IInvocation invocation);

        
    }
}
