﻿using System.ComponentModel;
using Zooyard.DataAnnotations;

namespace RpcContractHttp;

[ZooyardHttp("HttpHelloService", Url = "http://127.0.0.1:10010/hello")]
public interface IHelloService
{
    [GetMapping("CallNameVoid")]
    Task<string> CallNameVoid();
    [GetMapping("CallName")]
    Task CallName(string name);
    [GetMapping("CallVoid")]
    Task CallVoid();
    [GetMapping("Hello/{name}")]
    Task<string> Hello(string name);
    [GetMapping("SayHello/{name}")]
    Task<HelloResult> SayHello(string name);
    [PostMapping("ShowHello",Consumes = "application/json")]
    Task<string> ShowHello(HelloResult name);
}
