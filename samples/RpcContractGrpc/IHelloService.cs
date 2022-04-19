namespace RpcContractGrpc;

public interface IHelloService
{
    Task<NameResult> CallNameVoidAsync(Void voidData);
    Task<Void> CallNameAsync(NameResult name);
    Task<Void> CallVoidAsync(Void voidData);
    Task<NameResult> HelloAsync(NameResult name);
    Task<HelloResult> SayHelloAsync(NameResult name);
    Task<NameResult> ShowHelloAsync(HelloResult name);

    NameResult CallNameVoid(Void voidData);
    Void CallName(NameResult name);
    Void CallVoid(Void voidData);
    NameResult Hello(NameResult name);
    HelloResult SayHello(NameResult name);
    NameResult ShowHello(HelloResult name);
}
