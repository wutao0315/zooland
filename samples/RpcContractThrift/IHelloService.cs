using Zooyard.DataAnnotations;

namespace RpcContractThrift;

[ZooyardThrift("ThriftHelloService", typeof(HelloService.Client), Url = "Binary://127.0.0.1:9090")]
public interface IHelloService
{
    Task<string> CallNameVoidAsync();
    Task CallNameAsync(string name);
    Task CallVoidAsync();
    Task<string> HelloAsync(string name);
    Task<HelloData> SayHelloAsync(string name);
    Task<string> ShowHelloAsync(HelloData name);


    string CallNameVoid();
    void CallName(string name);
    void CallVoid();
    string Hello(string name);
    HelloData SayHello(string name);
    string ShowHello(HelloData name);
}
