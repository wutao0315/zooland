using Zooyard.DataAnnotations;

namespace RpcContractThrift;

[ZooyardThrift("ThriftHelloService", typeof(HelloService.Client), Url = "Binary://127.0.0.1:9090")]
public interface IHelloService
{
    Task<string> CallNameVoid();
    Task CallName(string name);
    Task CallVoid();
    Task<string> Hello(string name);
    Task<HelloData> SayHello(string name);
    Task<string> ShowHello(HelloData name);
}
