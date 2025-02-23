using Microsoft.AspNetCore.Mvc;
using RpcProviderCore.Models;

namespace RpcProviderCore.Controllers;

[Route("hello")]
[ApiController]
public class HelloController : ControllerBase
{
    public string ServiceName { get; set; } = "A";

    [HttpGet("callnamevoid")]
    public IActionResult CallNameVoid()
    {
        Console.WriteLine($"call CallNameVoid![{ServiceName}]");
        return Ok(new { code=0,msg="ok",data= $"CallNameVoid;From[{ServiceName}]" });
    }
    [HttpGet("callname")]
    public IActionResult CallName(string name)
    {
        Console.WriteLine($"{name} call CallName![{ServiceName}]");

        return Ok(new { code = 0, msg = "ok"});
    }
    [HttpGet("callvoid")]
    public IActionResult CallVoid()
    {
        Console.WriteLine($"call CallVoid![{ServiceName}]");
        return Ok(new { code = 0, msg = "ok" });
    }
    [HttpGet("hello/{name}")]
    public IActionResult Hello(string name)
    {
        Console.WriteLine($"{name} call Hello![{ServiceName}]");
        return Ok(new { code = 0, msg = "ok", data = $"hello {name};From[{ServiceName}]" });
    }
    [HttpGet("sayhello/{name}")]
    public IActionResult SayHello(string name)
    {
        Console.WriteLine($"{name} call SayHello![{ServiceName}]");
        var result = new HelloDTO.HelloModel
        {
            Name = $"{name};From[{ServiceName}]",
            Gender = "male",
            Head = $"head.png"
        };
        return Ok(new { code = 0, msg = "ok", data = result });
    }
    [HttpPost("showhello")]
    public IActionResult ShowHello(HelloDTO.HelloModel hello)
    {
        Console.WriteLine($"{hello.Name} call SayHello![{ServiceName}]");
        var result = $"name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head};From[{ServiceName}]";
        return Ok(new { code = 0, msg = "ok", data = result });
    }
}
