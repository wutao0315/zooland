﻿using Zooyard.DataAnnotations;

namespace RpcContractGrpc;

[ZooyardGrpc("GrpcHelloService", typeof(HelloService.HelloServiceClient), Url = "grpc://127.0.0.1:10008")]
public interface IHelloService
{
    Task<NameResult> CallNameVoid(Void voidData);
    Task<Void> CallName(NameResult name);
    Task<Void> CallVoid(Void voidData);
    Task<NameResult> Hello(NameResult name);
    Task<HelloResult> SayHello(NameResult name);
    Task<NameResult> ShowHello(HelloResult name);
}
