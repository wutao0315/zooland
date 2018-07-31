using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;
using Akka.Actor;
using System.Threading;

namespace Zooyard.Rpc.AkkaImpl
{
    public class AkkaInvoker : IInvoker
    {
        private ActorSystem Instance { get; set; }
        private URL Url { get; set; }
        private int Timeout { get; set; }
        public AkkaInvoker(ActorSystem instance,URL url,int timeout)
        {
            Instance = instance;
            this.Url = url;
            this.Timeout = timeout;
        }

        public IResult Invoke(IInvocation invocation)
        {
           
            //var types = (from item in invocation?.Arguments select item.GetType())?.ToArray();
            //var method = Instance.GetType().GetMethod(invocation.MethodInfo.Name, types);
            //var value = method.Invoke(Instance, invocation.Arguments);

            if (invocation.Arguments.Count()>1)
            {
                throw new Exception("akka not have muti parameters");
            }
            //var parameters = invocation.MethodInfo.GetParameters();
            //foreach (var item in parameters)
            //{
            //    //item.Name

            //}
            var greeting = Instance.ActorSelection($"{Url.Protocol}://{Url.ServiceInterface.Replace(".","-")}@{Url.Host}:{Url.Port}/{Url.Path}/{invocation.MethodInfo.Name}");

            object value = null;
            //invocation.MethodInfo.GetMethodBody()
            try
            {
                var cancelSource = new CancellationTokenSource();

                if (invocation.MethodInfo.ReturnType == typeof(void))
                {
                    if (invocation.Arguments.Count() == 0)
                    {
                        greeting.Tell(ActorRefs.Nobody);
                    }
                    else {
                        greeting.Tell(invocation.Arguments[0]);
                    }
                }
                else {
                    if (invocation.Arguments.Count() == 0)
                    {
                        value = greeting.Ask(ActorRefs.Nobody, TimeSpan.FromMilliseconds(Timeout), cancelSource.Token).GetAwaiter().GetResult();
                    }
                    else
                    {
                        value = greeting.Ask(invocation.Arguments[0], TimeSpan.FromMilliseconds(Timeout), cancelSource.Token).GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            

            var result = new RpcResult(value);
            return result;
        }
    }
}
