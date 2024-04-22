using Zooyard.Attributes;

namespace RpcContractGrpcNet;

[ZooyardGrpcNet("GrpcNetHelloService", typeof(HelloService.HelloServiceClient), Url = "http://127.0.0.1:10011")]
public interface IHelloNetService
{
    Task<NameResult> CallNameVoid(Void voidData);
    Task<Void> CallName(NameResult name);
    Task<Void> CallVoid(Void voidData);
    Task<NameResult> Hello(NameResult name);
    Task<HelloResult> SayHello(NameResult name);
    Task<NameResult> ShowHello(HelloResult name);
}
