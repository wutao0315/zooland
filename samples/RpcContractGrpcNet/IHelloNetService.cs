using Zooyard.Attributes;

namespace RpcContractGrpcNet;

[ZooyardGrpcNet("GrpcNetHelloService", typeof(HelloService.HelloServiceClient), Url = "http://127.0.0.1:10011")]
public interface IHelloNetService
{
    [RequestMapping(BaseReturnType = typeof(NameResult))]
    Task<NameResult> CallNameVoid(Void voidData);
    [RequestMapping(BaseReturnType = typeof(Void))]
    Task<Void> CallName(NameResult name);
    [RequestMapping(BaseReturnType = typeof(Void))]
    Task<Void> CallVoid(Void voidData);
    [RequestMapping(BaseReturnType = typeof(NameResult))]
    Task<NameResult> Hello(NameResult name);
    [RequestMapping(BaseReturnType = typeof(HelloResult))]
    Task<HelloResult> SayHello(NameResult name);
    [RequestMapping(BaseReturnType = typeof(NameResult))]
    Task<NameResult> ShowHello(HelloResult name);
}
