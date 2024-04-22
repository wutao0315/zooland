using System.ComponentModel;
using Zooyard.DynamicProxy;
using Zooyard.Rpc;

namespace Zooyard;

public interface IInterceptor
{
    public virtual async Task<string> UrlCall(string url, ProxyMethodResolverContext context)
    {
        await Task.CompletedTask;
        return url;
    }
    public virtual async Task BeforeCall(IInvocation invocation, RpcContext context)
    {
        await Task.CompletedTask;
    }
    public virtual async Task AfterCall<T>(IInvocation invocation, RpcContext context, IResult<T>? obj)
    {
        await Task.CompletedTask;
    }
    int Order { get; }
}
