using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RpcContractGrpc;
using Grpc.Core;

namespace RpcProviderCore
{
    public class HelloServiceGrpcImpl : HelloService.HelloServiceBase
    {
        private readonly IHelloRepository _helloRepository;
        public string ServiceName { get; set; } = "A";
        public HelloServiceGrpcImpl(IHelloRepository helloRepository) 
        {
            _helloRepository = helloRepository;
        }
        
        
        public override Task<RpcContractGrpc.Void> CallName(NameResult request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"from grpc CallName {request.Name} call Hello! [{ServiceName}]");

                return Task.FromResult(new RpcContractGrpc.Void());
            }
            catch (Exception ex)
            {
                var md = new Metadata();
                md.Add("err", ex.Message);
                context.WriteResponseHeadersAsync(md);
                return Task.FromResult(new RpcContractGrpc.Void());
            }
            
        }
        public override Task<NameResult> CallNameVoid(RpcContractGrpc.Void request, ServerCallContext context)
        {
            Console.WriteLine($"from grpc CallNameVoid call Hello! [{ServiceName}]");
            var name = $"from grpc hello CallNameVoid [{ServiceName}]";
            return Task.FromResult(new NameResult {
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

            var hello = _helloRepository.SayHello();
            request.Name = $"from grpc hello {request.Name} [{ServiceName}] {hello}";
            
            return Task.FromResult<NameResult>(request);
        }
        public override Task<RpcContractGrpc.HelloResult> SayHello(NameResult request, ServerCallContext context)
        {
            Console.WriteLine($"from grpc {request.Name} call SayHello! [{ServiceName}]");
            var hello = _helloRepository.SayHello();
            var result = new RpcContractGrpc.HelloResult
            {
                Name = $"from grpc {request.Name} [{ServiceName}]{hello}",
                Gender = "male",
                Head = "head.png"
            };

            return Task.FromResult<RpcContractGrpc.HelloResult>(result);
        }
        public override Task<NameResult> ShowHello(RpcContractGrpc.HelloResult request, ServerCallContext context)
        {
            Console.WriteLine($"from grpc {request.Name} call SayHello! [{ServiceName}]");
            var hello = _helloRepository.SayHello();
            var result = new NameResult
            {
                Name = $"from grpc name:{request.Name}；gender:{request.Gender}；avatar:{request.Head} [{ServiceName}]{hello}"
            };

            return Task.FromResult<NameResult>(result);
        }

    }
}
