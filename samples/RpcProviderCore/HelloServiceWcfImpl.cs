using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcContractWcf;
using System.ServiceModel;
using RpcContractWcf.HelloService;

namespace RpcProviderCore
{
    public class HelloServiceWcfImpl: IHelloServiceWcf
    {
        public string ServiceName { get; set; }

        public void CallName(string name)
        {
            Console.WriteLine($"from wcf {name} call CallName! [{ServiceName}]");
        }

        public async Task CallNameAsync(string name)
        {
            CallName(name);
            await Task.CompletedTask;
        }

        public string CallNameVoid()
        {
            Console.WriteLine($"from wcf call CallNameVoid! [{ServiceName}]");
            return $"from wcf CallNameVoid [{ServiceName}]";
        }

        public async Task<string> CallNameVoidAsync()
        {
            var result = CallNameVoid(); 
            await Task.CompletedTask;
            return result;
        }

        public void CallVoid()
        {
            Console.WriteLine($"from wcf call CallVoid! [{ServiceName}]");
        }

        public async Task CallVoidAsync()
        {
            CallVoid();
            await Task.CompletedTask;
        }

        public string Hello(string name)
        {
            Console.WriteLine($"from wcf {name} call Hello! [{ServiceName}]");
            return $"from wcf hello {name} [{ServiceName}]";
        }

        public async Task<string> HelloAsync(string name)
        {
            var result = Hello(name);
            await Task.CompletedTask;
            return result;
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

        public async Task<HelloResult> SayHelloAsync(string name)
        {
            var result = SayHello(name);
            await Task.CompletedTask;
            return result;
        }

        public string ShowHello(HelloResult hello)
        {
            Console.WriteLine($"from wcf {hello.Name} call SayHello! [{ServiceName}]");
            var result = $"from wcf name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head} [{ServiceName}]";
            return result;
        }

        public async Task<string> ShowHelloAsync(HelloResult name)
        {
            var result = ShowHello(name);
            await Task.CompletedTask;
            return result;
        }

        
    }
}
