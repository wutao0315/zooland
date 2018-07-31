using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcContractAkka;
using Akka.Actor;

namespace RpcProviderAutofac
{
    //public class HelloServiceAkkaImpl
    //{

    //}

    //public class GreetingActor : TypedActor, IHandle<GreetingMessage>
    //{
    //    public void Handle(GreetingMessage message)
    //    {
    //        Console.WriteLine("Hello world!");
    //    }
    //}
    public class CallNameVoidActor : UntypedActor
    {
        private string ServiceName { get; set; }
        public CallNameVoidActor(string serviceName)
        {
            this.ServiceName = serviceName;
        }

        protected override void OnReceive(object message)
        {
            Console.WriteLine($"from akka CallNameVoid {Self.Path.Name} received {message} [{ServiceName}]");
            if (message is Nobody)
            {
                Console.WriteLine($"from akka call CallNameVoid! [{ServiceName}]");

                var nameResult = new RpcContractAkka.NameResult
                {
                    Name = $" from akka CallNameVoid [{ServiceName}]"
                };
                Sender.Tell(nameResult, Self);
            }
            else
            {
                Unhandled(message);
            }

        }
    }
    public class CallNameActor : UntypedActor
    {
        private string ServiceName { get; set; }
        public CallNameActor(string serviceName)
        {
            this.ServiceName = serviceName;
        }

        protected override void OnReceive(object message)
        {
            Console.WriteLine($"from akka {Self.Path.Name} received {message} [{ServiceName}]");
            if (message is RpcContractAkka.NameResult)
            {
                var nameResult = (RpcContractAkka.NameResult)message;
                Console.WriteLine($"from akka {nameResult.Name} call CallName! [{ServiceName}]");

                nameResult.Name = $" from akka CallName {nameResult.Name} [{ServiceName}]";
            }
            else
            {
                Unhandled(message);
            }

        }
    }
    public class CallVoidActor : UntypedActor
    {
        private string ServiceName { get; set; }
        public CallVoidActor(string serviceName)
        {
            this.ServiceName = serviceName;
        }

        protected override void OnReceive(object message)
        {
            Console.WriteLine($"from akka {Self.Path.Name} received {message} [{ServiceName}]");
            if (message is Nobody)
            {
                Console.WriteLine($"from akka call CallVoid! [{ServiceName}]");
            }
            else
            {
                Unhandled(message);
            }

        }
    }
    public class HelloActor : UntypedActor
    {
        private string ServiceName { get; set; }
        public HelloActor(string serviceName)
        {
            this.ServiceName = serviceName;
        }

        protected override void OnReceive(object message)
        {
            Console.WriteLine($"from akka {Self.Path.Name} received {message} [{ServiceName}]");
            if (message is RpcContractAkka.NameResult)
            {
                var nameResult = (RpcContractAkka.NameResult)message;
                Console.WriteLine($"from akka {nameResult.Name} call Hello! [{ServiceName}]");

                nameResult.Name = $" from akka hello {nameResult.Name} [{ServiceName}]";
                Sender.Tell(nameResult, Self);
            }
            else {
                Unhandled(message);
            }
           
        }
    }
    public class SayHelloActor : UntypedActor
    {
        private string ServiceName { get; set; }
        public SayHelloActor(string serviceName)
        {
            this.ServiceName = serviceName;
        }

        protected override void OnReceive(object message)
        {
            Console.WriteLine($"from akka {Self.Path.Name} received {message} [{ServiceName}]");
            if (message is RpcContractAkka.NameResult)
            {
                var nameResult = (RpcContractAkka.NameResult)message;
                Console.WriteLine($"from akka {nameResult.Name} call SayHello! [{ServiceName}]");
                var result = new RpcContractAkka.HelloResult
                {
                    Name = $"from akka {nameResult.Name} [{ServiceName}]",
                    Gender = "male",
                    Head = "head.png"
                };
                Sender.Tell(result, Self);
            }
            else
            {
                Unhandled(message);
            }
        }
    }
    public class ShowHelloActor : UntypedActor
    {
        private string ServiceName { get; set; }
        public ShowHelloActor(string serviceName)
        {
            this.ServiceName = serviceName;
        }

        protected override void OnReceive(object message)
        {
            Console.WriteLine($"from akka {Self.Path.Name} received {message} [{ServiceName}]");

            if (message is RpcContractAkka.HelloResult)
            {
                var hello = (RpcContractAkka.HelloResult)message;

                Console.WriteLine($"from akka {hello.Name} call SayHello! [{ServiceName}]");

                var result=new RpcContractAkka.NameResult
                {
                   Name=$"from akka name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head} [{ServiceName}]"
                };

                Sender.Tell(result, Self);
            }
            else
            {
                Unhandled(message);
            }


        }
    }
    
}
