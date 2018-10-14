using Zooyard.Core;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfInvoker : IInvoker
    {
        private object Instance { get; set; }
        public WcfInvoker(object instance)
        {
            Instance = instance;
        }

        public IResult Invoke(IInvocation invocation)
        {
            var method = Instance.GetType().GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            var value = method.Invoke(Instance, invocation.Arguments);
            return new RpcResult(value);
        }
    }
}
