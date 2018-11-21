using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Logging;
using Zooyard.Core;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.AkkaImpl
{
    public class AkkaClientPool : AbstractClientPool
    {
        public const string TIMEOUT_KEY = "akka_timeout";
        public const int DEFAULT_TIMEOUT = 5000;

        private readonly string _actorConfig;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        public AkkaClientPool(string actorConfig, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _actorConfig = actorConfig;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<AkkaClientPool>();
        }

        //public string TheActorConfig { get; set; } = @"akka {  
        //        actor {
        //            provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
        //        }
        //        remote {
        //            helios.tcp {
        //                transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
        //                applied-adapters = []
        //                transport-protocol = tcp
        //                port = 0
        //                hostname = localhost
        //            }
        //        }
        //    }";

        protected override IClient CreateClient(URL url)
        {
            //获得transport参数,用于反射实例化
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);
            
            var config = ConfigurationFactory.ParseString(_actorConfig);

            var system = ActorSystem.Create($"client-{url.ServiceInterface.Replace(".", "-")}", config);
            
            return new AkkaClient(system, url, timeout, _loggerFactory);
        }
    }
}
