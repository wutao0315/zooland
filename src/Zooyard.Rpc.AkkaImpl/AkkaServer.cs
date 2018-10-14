using Akka.Actor;
using Akka.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.AkkaImpl
{
    public class AkkaServer : AbstractServer
    {
        public string TheActorConfig { get; set; } = @"
        akka {  
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            }
            remote {
                dot-netty.tcp {
                    port = 10011
                    hostname = 0.0.0.0
                    public-hostname = localhost 
                }
            }
        }
        ";
        public string TheActorName { get; set; }
        private ActorSystem TheActorSystem { get; set; }
        public IDictionary<string, ZooyardActor> TheActors { get; set; }
        
        public override void DoExport()
        {

            // Use this for a multithreaded server
            // server = new TThreadPoolServer(processor, serverTransport);
            var config = ConfigurationFactory.ParseString(TheActorConfig);
            
            TheActorSystem = ActorSystem.Create(TheActorName.Replace(".","-"), config);
            
            foreach (var item in TheActors)
            {
                TheActorSystem.ActorOf(Props.Create(item.Value.ActorType,(item.Value.Args??new List<object>()).ToArray()), item.Key);
            }

            Console.WriteLine($"Starting the akka server ...");
            //开启服务
            //TheServer.Serve();
            //向注册中心发送服务注册信息

        }

        public override void DoDispose()
        {
            //向注册中心发送注销请求

            TheActorSystem.Dispose();
        }
    }
    public class ZooyardActor
    {
        public Type ActorType { get; set; }
        public IList<object> Args { get; set; }
    }

}
