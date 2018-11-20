using Akka.Actor;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using Zooyard.Core;

namespace Zooyard.Rpc.AkkaImpl
{
    public class AkkaInvoker : IInvoker
    {
        private readonly ActorSystem _instance;
        private readonly URL _url;
        private readonly int _timeout;
        private readonly ILogger _logger;
        public AkkaInvoker(ActorSystem instance,URL url,int timeout,ILoggerFactory loggerFactory)
        {
            _instance = instance;
            _url = url;
            _timeout = timeout;
            _logger = loggerFactory.CreateLogger<AkkaInvoker>();
        }

        public IResult Invoke(IInvocation invocation)
        {
            if (invocation.Arguments.Count()>1)
            {
                throw new Exception("akka not have muti parameters");
            }

            var greeting = _instance.ActorSelection($"{_url.Protocol}://{_url.ServiceInterface.Replace(".","-")}@{_url.Host}:{_url.Port}/{_url.Path}/{invocation.MethodInfo.Name}");

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
                        value = greeting.Ask(ActorRefs.Nobody, TimeSpan.FromMilliseconds(_timeout), cancelSource.Token).GetAwaiter().GetResult();
                    }
                    else
                    {
                        value = greeting.Ask(invocation.Arguments[0], TimeSpan.FromMilliseconds(_timeout), cancelSource.Token).GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }

            _logger.LogInformation($"Invoke:{invocation.MethodInfo.Name}");
            var result = new RpcResult(value);
            return result;
        }
    }
}
