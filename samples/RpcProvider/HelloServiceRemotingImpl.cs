using RpcContractRemoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcProvider
{
    public class HelloServiceRemotingImpl : MarshalByRefObject, IHelloService
    {
        public string ServiceName { get; set; }

        public void CallName(string name)
        {
            Console.WriteLine($"from remoting {name} call CallName![{ServiceName}]");
        }

        public string CallNameVoid()
        {
            Console.WriteLine($"from remoting call CallNameVoid![{ServiceName}]");
            return $"from remoting CallNameVoid;From[{ServiceName}]";
        }

        public void CallVoid()
        {
            Console.WriteLine($"from remoting call CallVoid![{ServiceName}]");
        }

        public string Hello(string name)
        {
            Console.WriteLine($"from remoting {name} call Hello![{ServiceName}]");
            return $"from remoting hello {name};From[{ServiceName}]";
        }

        public RpcContractRemoting.HelloResult SayHello(string name)
        {
            Console.WriteLine($"from remoting {name} call SayHello![{ServiceName}]");
            var result = new RpcContractRemoting.HelloResult
            {
                Name = $"from remoting {name};From[{ServiceName}]",
                Gender = "male",
                Head = $"head.png"
            };
            return result;
        }

        public string ShowHello(RpcContractRemoting.HelloResult hello)
        {
            Console.WriteLine($"from remoting {hello.Name} call SayHello![{ServiceName}]");
            var result = $"from remoting name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head};From[{ServiceName}]";
            return result;
        }
        
    }
}
