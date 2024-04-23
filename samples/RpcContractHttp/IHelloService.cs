using System.ComponentModel;
using Zooyard;
using Zooyard.Attributes;

namespace RpcContractHttp;

[ZooyardHttp("HttpHelloService", Url = "http://127.0.0.1:10010/{domain}/hello", Config ="domain=Appsettings:Domain@testdomain")]
public interface IHelloService
{
    [GetMapping("CallNameVoid")]
    Task<string?> CallNameVoid();

    [GetMapping("CallName")]
    void CallName(string name);

    [GetMapping("CallVoid")]
    Task CallVoid();

    [GetMapping("Hello/{name}")]
    Task<string?> Hello(string name);

    [GetMapping("SayHello/{name}")]
    Task<HelloResult?> SayHello(string name);

    [PostMapping("ShowHello", Consumes = "application/json")]
    Task<string?> ShowHello(HelloResult name);

    [PostMapping("getpage", Consumes = "application/json")]
    Task<Result<HelloResult>?> GetPage(string name);
    
}
