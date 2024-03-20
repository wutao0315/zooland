using System.ComponentModel;
using Zooyard.Rpc;

namespace Zooyard;

public interface IInterceptor
{
    Task BeforeCall(IInvocation invocation, RpcContext context);
    Task AfterCall<T>(IInvocation invocation, RpcContext context, out IResult<T>? obj);
    int Order { get; }
}
