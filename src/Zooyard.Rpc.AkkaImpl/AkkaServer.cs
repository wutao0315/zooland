using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.AkkaImpl
{
    public class AkkaServer : AbstractServer
    {
        private readonly ActorSystem _actorSystem;
        private readonly IDictionary<string, ZooyardActor> _actors;

        //private readonly string _actorName;
        //private readonly string _actorConfig = @"
        //akka {  
        //    actor {
        //        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
        //    }
        //    remote {
        //        dot-netty.tcp {
        //            port = 10011
        //            hostname = 0.0.0.0
        //            public-hostname = localhost 
        //        }
        //    }
        //}
        //";

        private readonly ILogger _logger;
        public AkkaServer(ActorSystem actorSystem, IDictionary<string, ZooyardActor> actors, ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AkkaServer>();
            _actorSystem = actorSystem;
            _actors = actors;
        }
        public AkkaServer(string actorName, string actorConfig, IDictionary<string, ZooyardActor> actors, ILoggerFactory loggerFactory) 
            : this(ActorSystem.Create(actorName.Replace(".", "-"), ConfigurationFactory.ParseString(actorConfig)), actors, loggerFactory)
        {
        }

       
        public override void DoExport()
        {
            foreach (var item in _actors)
            {
                _actorSystem.ActorOf(Props.Create(item.Value.ActorType,(item.Value.Args??new List<object>()).ToArray()), item.Key);
            }

            _logger.LogInformation($"Started the akka server ...");
            Console.WriteLine($"Started the akka server ...");
        }

        public override void DoDispose()
        {
            _actorSystem.Dispose();
        }
    }
    public class ZooyardActor
    {
        public Type ActorType { get; set; }
        public IList<object> Args { get; set; }
    }

}
