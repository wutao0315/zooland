using RpcProviderWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace RpcProviderWeb.Controllers
{
    public class HelloController : ApiController
    {
        [HttpHead]
        public string Head()
        {
            return "rpc is ok";
        }

        public string ServiceName { get; set; } = "A";

        [HttpGet]
        public string CallNameVoid()
        {
            Console.WriteLine($"call CallNameVoid![{ServiceName}]");
            return $"CallNameVoid;From[{ServiceName}]";
        }
        [HttpGet]
        public void CallName(string name)
        {
            Console.WriteLine($"{name} call CallName![{ServiceName}]");
        }
        [HttpGet]
        public void CallVoid()
        {
            Console.WriteLine($"call CallVoid![{ServiceName}]");
        }
        [HttpGet]
        public string Hello(string name)
        {
            Console.WriteLine($"{name} call Hello![{ServiceName}]");
            return $"hello {name};From[{ServiceName}]";
        }
        [HttpGet]
        public HelloDTO.HelloModel SayHello(string name)
        {
            Console.WriteLine($"{name} call SayHello![{ServiceName}]");
            var result = new HelloDTO.HelloModel
            {
                Name = $"{name};From[{ServiceName}]",
                Gender = "male",
                Head = $"head.png"
            };
            return result;
        }
        [HttpPost]
        public string ShowHello(HelloDTO.HelloModel hello)
        {
            Console.WriteLine($"{hello.Name} call SayHello![{ServiceName}]");
            var result = $"name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head};From[{ServiceName}]";
            return result;
        }
    }
}
