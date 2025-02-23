using Zooyard.Attributes;

namespace RpcContractHttp;

//[ZooyardHttp("HttpHelloService", Url = "http://127.0.0.1:10010/{domain}/hello", Config ="domain=Appsettings:Domain@testdomain")]
[ZooyardHttp("HttpHelloService", Url = "http://127.0.0.1:10010/hello")]
public interface IHelloService
{
    [GetMapping("callnamevoid")]
    Task<string?> CallNameVoid();

    [GetMapping("callname")]
    void CallName(string name);

    [GetMapping("callvoid")]
    Task CallVoid();

    [GetMapping("hello/{name}")]
    Task<string?> Hello(string name);

    [GetMapping("sayhello/{name}")]
    Task<HelloResult?> SayHello(string name);

    [PostMapping("showhello", Consumes = "application/json")]
    Task<string?> ShowHello(HelloResult name);

    [PostMapping("getpage", Consumes = "application/json")]
    Task<Result<HelloResult>?> GetPage(string name);
    
}
