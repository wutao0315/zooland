using Akka.Actor;
using System;
using System.Linq;
using System.Threading;
using Zooyard.Core;

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
            if (invocation.Arguments.Count()>1)
            {
                throw new Exception("akka not have muti parameters");
            }

            var greeting = Instance.ActorSelection($"{Url.Protocol}://{Url.ServiceInterface.Replace(".","-")}@{Url.Host}:{Url.Port}/{Url.Path}/{invocation.MethodInfo.Name}");

            object value = null;
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
