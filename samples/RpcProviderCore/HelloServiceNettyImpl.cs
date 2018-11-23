using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcContractNetty;

namespace RpcProviderCore
{
    public class HelloServiceNettyImpl: IHelloService
    {

        public string ServiceName { get; set; } = "A";

        public string CallNameVoid()
        {
            Console.WriteLine($"call CallNameVoid![{ServiceName}]");
            return $"CallNameVoid;From[{ServiceName}]";
        }
        public void CallName(string name)
        {
            Console.WriteLine($"{name} call CallName![{ServiceName}]");
        }

        public void CallVoid()
        {
            Console.WriteLine($"call CallVoid![{ServiceName}]");
        }

        public string Hello(string name)
        {
            Console.WriteLine($"{name} call Hello![{ServiceName}]");
            return $"hello {name};From[{ServiceName}]";
        }

        public RpcContractNetty.HelloResult SayHello(string name)
        {
            Console.WriteLine($"{name} call SayHello![{ServiceName}]");
            var result = new RpcContractNetty.HelloResult
            {
                Name = $"{name};From[{ServiceName}]",
                Gender = "male",
                Head = $"head.png"
            };
            return result;
        }

        public string ShowHello(RpcContractNetty.HelloResult hello)
        {
            Console.WriteLine($"{hello.Name} call SayHello![{ServiceName}]");
            var result = $"name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head};From[{ServiceName}]";
            return result;
        }
    }
}
