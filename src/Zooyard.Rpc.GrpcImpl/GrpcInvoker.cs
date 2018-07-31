using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.GrpcImpl
{
    public class GrpcInvoker : IInvoker
    {
        private object Instance { get; set; }
        private int ClientTimeout { get; set; }
        public GrpcInvoker(object instance,int clientTimeout)
        {
            Instance = instance;
            ClientTimeout=clientTimeout;
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
                .WithDeadline(DateTime.UtcNow.AddMilliseconds(ClientTimeout));
            var method = Instance.GetType().GetMethod(invocation.MethodInfo.Name, paraTypes);
            var value = method.Invoke(Instance, parasPlus);

            var result = new RpcResult(value);
            return result;
        }
    }
}
