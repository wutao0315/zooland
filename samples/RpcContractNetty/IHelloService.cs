using Zooyard.DataAnnotations;

namespace RpcContractNetty;

[ZooyardNetty("NettyHelloService", Url = "socket://127.0.0.1:12121?cluster=failfast")]
public interface IHelloService
{
    Task<string> CallNameVoidAsync();
    Task CallNameAsync(string name);
    Task CallVoidAsync();
    Task<string> HelloAsync(string name);
    Task<HelloResult> SayHelloAsync(string name);
    Task<string> ShowHelloAsync(HelloResult name);
}
