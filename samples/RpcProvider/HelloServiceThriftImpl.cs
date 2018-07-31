using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcContractThrift;

namespace RpcProvider
{
    public class HelloServiceThriftImpl : HelloService.Iface
    {
        public string ServiceName { get; set; }

        public void CallName(string name)
        {
            Console.WriteLine($"from thrift {name} call CallName![{ServiceName}]");
        }

        public string CallNameVoid()
        {
            Console.WriteLine($"from thrift call CallNameVoid![{ServiceName}]");
            return $"from thrift CallNameVoid;From[{ServiceName}]";
        }

        public void CallVoid()
        {
            Console.WriteLine($"from thrift call CallVoid![{ServiceName}]");
        }

        public string Hello(string name)
        {
            Console.WriteLine($"from thrift {name} call Hello![{ServiceName}]");
            return $"from thrift hello {name};From[{ServiceName}]";
        }

        public RpcContractThrift.HelloResult SayHello(string name)
        {
            Console.WriteLine($"from thrift {name} call SayHello![{ServiceName}]");
            var result = new RpcContractThrift.HelloResult
            {
                Name = $"from thrift {name};From[{ServiceName}]",
                Gender = "male",
                Head = $"head.png"
            };
            return result;
        }

        public string ShowHello(RpcContractThrift.HelloResult hello)
        {
            Console.WriteLine($"from thrift {hello.Name} call SayHello![{ServiceName}]");
            var result = $"from thrift name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head};From[{ServiceName}]";
            return result;
        }
    }
}
