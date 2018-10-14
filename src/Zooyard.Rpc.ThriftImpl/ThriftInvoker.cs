using System;
using Zooyard.Core;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftInvoker : IInvoker
    {
        private IDisposable Instance { get; set; }
        public ThriftInvoker(IDisposable instance)
        {
            Instance = instance;
        }

        public IResult Invoke(IInvocation invocation)
        {
            //var methodName = $"{invocation.MethodInfo.Name}Async";
            //var argumentTypes = new List<Type>(invocation.ArgumentTypes);
            //argumentTypes.Add(typeof(CancellationToken));
            //var arguments = new List<object>(invocation.Arguments);
            //arguments.Add(CancellationToken.None);

            //var method = Instance.GetType().GetMethod(methodName, argumentTypes.ToArray());
            //var value = method.Invoke(Instance, arguments.ToArray());
            //return new RpcResult(value);

            var method = Instance.GetType().GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            var value = method.Invoke(Instance, invocation.Arguments);
            return new RpcResult(value);
        }
    }
}
