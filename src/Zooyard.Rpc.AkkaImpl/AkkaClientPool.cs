
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;
using Zooyard.Rpc.Support;
using Akka.Configuration;
using Akka.Actor;

namespace Zooyard.Rpc.AkkaImpl
{
    public class AkkaClientPool : AbstractClientPool
    {
        public const string TIMEOUT_KEY = "akka_timeout";
        public const int DEFAULT_TIMEOUT = 5000;

        public string TheActorConfig { get; set; } = @"akka {  
                actor {
                    provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                }
                remote {
                    helios.tcp {
                        transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                        applied-adapters = []
                        transport-protocol = tcp
                        port = 0
                        hostname = localhost
                    }
                }
            }";

        protected override IClient CreateClient(URL url)
        {
            //获得transport参数,用于反射实例化
            var timeout = url.GetParameter(TIMEOUT_KEY, DEFAULT_TIMEOUT);

            //var config = ConfigurationFactory.ParseString(TheActorConfig.Replace("{0}", url.Port.ToString()).Replace("{1}",url.Host));
            var config = ConfigurationFactory.ParseString(TheActorConfig);

            var system = ActorSystem.Create($"client-{url.ServiceInterface.Replace(".", "-")}", config);
            
            return new AkkaClient(system, url, timeout);
        }
    }
}
