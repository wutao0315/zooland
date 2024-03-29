﻿using Microsoft.AspNetCore.Mvc;
using RpcProviderCore.Models;

namespace RpcProviderCore.Controllers;

[Route("hello")]
[ApiController]
public class HelloController : ControllerBase
{
    public string ServiceName { get; set; } = "A";

    [HttpGet]
    [Route("callnamevoid")]
    public string CallNameVoid()
    {
        Console.WriteLine($"call CallNameVoid![{ServiceName}]");
        return $"CallNameVoid;From[{ServiceName}]";
    }
    [HttpGet]
    [Route("callname")]
    public void CallName(string name)
    {
        Console.WriteLine($"{name} call CallName![{ServiceName}]");
    }
    [HttpGet]
    [Route("callvoid")]
    public void CallVoid()
    {
        Console.WriteLine($"call CallVoid![{ServiceName}]");
    }
    [HttpGet]
    [Route("hello/{name}")]
    public string Hello(string name)
    {
        Console.WriteLine($"{name} call Hello![{ServiceName}]");
        return $"hello {name};From[{ServiceName}]";
    }
    [HttpGet]
    [Route("sayhello/{name}")]
    public HelloDTO.HelloModel SayHello(string name)
    {
        Console.WriteLine($"{name} call SayHello![{ServiceName}]");
        var result = new HelloDTO.HelloModel
        {
            Name = $"{name};From[{ServiceName}]",
            Gender = "male",
            Head = $"head.png"
        };
        return result;
    }
    [HttpPost]
    [Route("showhello")]
    public string ShowHello(HelloDTO.HelloModel hello)
    {
        Console.WriteLine($"{hello.Name} call SayHello![{ServiceName}]");
        var result = $"name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head};From[{ServiceName}]";
        return result;
    }
}
