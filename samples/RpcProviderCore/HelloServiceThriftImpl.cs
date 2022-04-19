﻿using RpcContractThrift;

namespace RpcProviderCore;

public class HelloServiceThriftImpl : HelloService.IAsync
{
    public string ServiceName { get; set; }

    public async Task CallNameAsync(string name, CancellationToken cancellationToken)
    {
        Console.WriteLine($"from thrift {name} call CallName![{ServiceName}]");
        await Task.CompletedTask;
    }

    public async Task<string> CallNameVoidAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"from thrift call CallNameVoid![{ServiceName}]");
        await Task.CompletedTask;
        return $"from thrift CallNameVoid;From[{ServiceName}]";
    }

    public async Task CallVoidAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"from thrift call CallVoid![{ServiceName}]");
        await Task.CompletedTask;
    }

    public async Task<string> HelloAsync(string name, CancellationToken cancellationToken)
    {
        Console.WriteLine($"from thrift {name} call Hello![{ServiceName}]");
        var result = $"from thrift hello {name};From[{ServiceName}]";
        await Task.CompletedTask;
        return result; 
    }

    public async Task <HelloData> SayHelloAsync(string name, CancellationToken cancellationToken)
    {
        Console.WriteLine($"from thrift {name} call SayHello![{ServiceName}]");

        var result = new HelloData
        {
            Name = $"from thrift {name};From[{ServiceName}]",
            Gender = "male",
            Head = $"head.png"
        };
        return await Task.FromResult(result);
    }

    public async Task<string> ShowHelloAsync(HelloData hello, CancellationToken cancellationToken)
    {
        
        Console.WriteLine($"from thrift {hello.Name} call SayHello![{ServiceName}]");
        var result = $"from thrift name:{hello.Name}；gender:{hello.Gender}；avatar:{hello.Head};From[{ServiceName}]";
        return await Task.FromResult(result);
    }
}
