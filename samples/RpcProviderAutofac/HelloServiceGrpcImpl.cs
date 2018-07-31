using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcContractGrpc;
using Grpc.Core;

namespace RpcProviderAutofac
{
    public class HelloServiceGrpcImpl : HelloService.HelloServiceBase
    {
        public string ServiceName { get; set; }
        public override Task<RpcContractGrpc.Void> CallName(NameResult request, ServerCallContext context)
        {
            Console.WriteLine($"from grpc CallName {request.Name} call Hello! [{ServiceName}]");

            return Task.FromResult<RpcContractGrpc.Void>(new RpcContractGrpc.Void ());
        }
        public override Task<NameResult> CallNameVoid(RpcContractGrpc.Void request, ServerCallContext context)
        {
            Console.WriteLine($"from grpc CallNameVoid call Hello! [{ServiceName}]");
            var name = $"from grpc hello CallNameVoid [{ServiceName}]";
            return Task.FromResult<NameResult>(new NameResult {
                 Name= name
            });
        }
        public override Task<RpcContractGrpc.Void> CallVoid(RpcContractGrpc.Void request, ServerCallContext context)
        {
            Console.WriteLine($"from grpc CallVoid call Hello! [{ServiceName}]");
            return Task.FromResult<RpcContractGrpc.Void>(new RpcContractGrpc.Void());
        }
        public override Task<NameResult> Hello(NameResult request, ServerCallContext context)
        {
            
            Console.WriteLine($"from grpc {request.Name} call Hello! [{ServiceName}]");

            request.Name = $"from grpc hello {request.Name} [{ServiceName}]";
            
            return Task.FromResult<NameResult>(request);
        }
        public override Task<RpcContractGrpc.HelloResult> SayHello(NameResult request, ServerCallContext context)
        {
            Console.WriteLine($"from grpc {request.Name} call SayHello! [{ServiceName}]");
            var result = new RpcContractGrpc.HelloResult
            {
                Name = $"from grpc {request.Name} [{ServiceName}]",
                Gender = "male",
                Head = "head.png"
            };

            return Task.FromResult<RpcContractGrpc.HelloResult>(result);
        }
        public override Task<NameResult> ShowHello(RpcContractGrpc.HelloResult request, ServerCallContext context)
        {
            Console.WriteLine($"from grpc {request.Name} call SayHello! [{ServiceName}]");
            var result = new NameResult
            {
                Name = $"from grpc name:{request.Name}；gender:{request.Gender}；avatar:{request.Head} [{ServiceName}]"
            };

            return Task.FromResult<NameResult>(result);
        }

    }
}
