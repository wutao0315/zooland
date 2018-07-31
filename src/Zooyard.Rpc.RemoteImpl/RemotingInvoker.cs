using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.RemotingImpl
{
    public class RemotingInvoker : IInvoker
    {
        private object Instance { get; set; }
        public RemotingInvoker(object instance)
        {
            Instance = instance;
        }

        public IResult Invoke(IInvocation invocation)
        {
            var method = invocation.TargetType.GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            var value = method.Invoke(Instance, invocation.Arguments);
            return new RpcResult(value);
        }
    }
}
