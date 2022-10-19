using Zooyard.DataAnnotations;

namespace RpcContractNetty;

[NettyProxy("NettyHelloService")]
public interface IHelloService
{
    Task<string> CallNameVoidAsync();
    Task CallNameAsync(string name);
    Task CallVoidAsync();
    Task<string> HelloAsync(string name);
    Task<HelloResult> SayHelloAsync(string name);
    Task<string> ShowHelloAsync(HelloResult name);

    string CallNameVoid();
    void CallName(string name);
    void CallVoid();
    string Hello(string name);
    HelloResult SayHello(string name);
    string ShowHello(HelloResult name);
}
