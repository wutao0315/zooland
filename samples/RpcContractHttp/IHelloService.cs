using System.ComponentModel;
using Zooyard.DataAnnotations;

namespace RpcContractHttp;

[HttpProxy("HttpHelloService", BaseUrl ="/hello")]
public interface IHelloService
{
    [GetMapping("CallNameVoid")]
    Task<string> CallNameVoidAsync();
    [GetMapping("CallName")]
    Task CallNameAsync(string name);

    [GetMapping("CallVoid")]
    Task CallVoidAsync();
    [GetMapping("Hello/{name}")]
    Task<string> HelloAsync(string name);
    [GetMapping("SayHello")]
    Task<HelloResult> SayHelloAsync(string name);
    [PostMapping("ShowHello",Consumes = "application/json")]
    Task<string> ShowHelloAsync(HelloResult name);

    [GetMapping("CallNameVoid")]
    string CallNameVoid();
    [GetMapping("CallName")]
    void CallName(string name);
    [GetMapping("CallVoid")]
    void CallVoid();
    [GetMapping("Hello")]
    string Hello(string name);
    [GetMapping("SayHello/{name}")]
    HelloResult SayHello(string name);
    [PostMapping("ShowHello", Consumes = "application/json")]
    string ShowHello(HelloResult name);
}
