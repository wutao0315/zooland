﻿using RpcContractNetty;

namespace RpcProviderCore;

public class HelloServiceNettyImpl: IHelloService
{

    public string ServiceName { get; set; } = "A";

    public async Task<string> CallNameVoidAsync()
    {
        await Task.CompletedTask;
        Console.WriteLine($"call CallNameVoid![{ServiceName}]");
        return $"CallNameVoid;From[{ServiceName}]";
    }
    public async Task CallNameAsync(string name)
    {
        await Task.CompletedTask;
        Console.WriteLine($"{name} call CallName![{ServiceName}]");
    }

    public async Task CallVoidAsync()
    {
        await Task.CompletedTask;
        Console.WriteLine($"call CallVoid![{ServiceName}]");
    }

    public async Task<string> HelloAsync(string name)
    {
        await Task.CompletedTask;
        Console.WriteLine($"{name} call Hello![{ServiceName}]");
        return $"hello {name};From[{ServiceName}]";
    }

    public async Task<RpcContractNetty.HelloResult> SayHelloAsync(string name)
    {
        await Task.CompletedTask;
        Console.WriteLine($"{name} call SayHello![{ServiceName}]");
        var result = new RpcContractNetty.HelloResult
        {
            Name = $"{name};From[{ServiceName}]",
            Gender = "male",
            Head = $"head.png"
        };
        return result;
    }

    public async Task<string> ShowHelloAsync(RpcContractNetty.HelloResult hello)
    {
        await Task.CompletedTask;
        Console.WriteLine($"{hello.Name} call SayHello![{ServiceName}]");
        var result = $"name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head};From[{ServiceName}]";
        return result;
    }
}
