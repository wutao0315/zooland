using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcContractWcf;
using System.ServiceModel;

namespace RpcProviderAutofac
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class HelloServiceWcfImpl: IHelloServiceWcf
    {
        public string ServiceName { get; set; }

        public void CallName(string name)
        {
            Console.WriteLine($"from wcf {name} call CallName! [{ServiceName}]");
        }

        public string CallNameVoid()
        {
            Console.WriteLine($"from wcf call CallNameVoid! [{ServiceName}]");
            return $"from wcf CallNameVoid [{ServiceName}]";
        }

        public void CallVoid()
        {
            Console.WriteLine($"from wcf call CallVoid! [{ServiceName}]");
        }

        public string Hello(string name)
        {
            Console.WriteLine($"from wcf {name} call Hello! [{ServiceName}]");
            return $"from wcf hello {name} [{ServiceName}]";
        }

        public HelloResult SayHello(string name)
        {
            Console.WriteLine($"from wcf {name} call SayHello! [{ServiceName}]");
            var result = new HelloResult
            {
                Name = $"from wcf {name} [{ServiceName}]",
                Gender = "male",
                Head = "head.png"
            };
            return result;
        }

        public string ShowHello(HelloResult hello)
        {
            Console.WriteLine($"from wcf {hello.Name} call SayHello! [{ServiceName}]");
            var result = $"from wcf name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head} [{ServiceName}]";
            return result;
        }
    }
}
