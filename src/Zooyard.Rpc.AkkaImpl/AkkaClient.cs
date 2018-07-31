
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
    public class AkkaClient : AbstractClient
    {
        public override URL Url { get; }
        private ActorSystem TheActorSystem { get; set; }
        private int Timeout { get; set; }

        public AkkaClient(ActorSystem actorSystem,URL url,int timeout)
        {
            this.TheActorSystem = actorSystem;
            this.Url = url;
            this.Timeout = timeout;
        }

        public override IInvoker Refer()
        {
            //var config = ConfigurationFactory.ParseString(@"
            //akka {  
            //    actor {
            //        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            //    }
            //    remote {
            //        helios.tcp {
            //            transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
            //            applied-adapters = []
            //            transport-protocol = tcp
            //            port = 0
            //            hostname = localhost
            //        }
            //    }
            //}
            //");
            //using (var system = ActorSystem.Create(Url.ServiceInterface, config))
            //{
            //    var greeting = system.ActorSelection($"{Url.Protocol}://{Url.ServiceInterface}@{Url.Host}:{Url.Port}/{Url.Path}");

            //    while (true)
            //    {
            //        var input = Console.ReadLine();
            //        if (input.Equals("sayHello"))
            //        {

            //            greeting.Tell(new GreetingMessage());
            //        }
            //    }
            //}
            //var greeting = TheActorSystem.ActorSelection($"{Url.Protocol}://{Url.ServiceInterface}@{Url.Host}:{Url.Port}/{Url.Path}");

            //greeting.Ask();
            //if (TheTransport != null)
            //{
            //    TheTransport.Open();
            //}
            //thrift client service

            return new AkkaInvoker(TheActorSystem, Url, Timeout);
        }

        public override void Open()
        {

        }

        public override void Close()
        {
            TheActorSystem.Dispose();

        }
        public override void Dispose()
        {
            TheActorSystem.Dispose();
        }
    }
}
