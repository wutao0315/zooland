using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var method = Instance.GetType().GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            var value = method.Invoke(Instance, invocation.Arguments);
            return new RpcResult(value);
            //RpcResult result;
            //try
            //{
            //    var method = Instance.GetType().GetMethod(invocation.MethodInfo.Name, invocation.ArgumentTypes);
            //    var value = method.Invoke(Instance, invocation.Arguments);
            //    result = new RpcResult(value);
            //}
            //catch (Exception ex)
            //{
            //    result = new RpcResult(ex);
            //}
            //return result;
        }
    }
}
