using Microsoft.Extensions.Logging;
using Zooyard.DynamicProxy;
using Zooyard.Rpc;

namespace Zooyard;

public interface IResult
{
    long ElapsedMilliseconds { get; set; }
    bool HasException { get; }
    Exception? Exception { get; }
}


public interface IResult<T> : IResult
{
    T? Value { get; }
}

public record RpcResult<T> : IResult<T>
{
    public T? Value { get; private set; }
    public long ElapsedMilliseconds { get; set; }
    public Exception? Exception { get; private set; }

    public RpcResult()
    {
    }

    public RpcResult(T? result)
    {
        Value = result;
    }

    public RpcResult(Exception? exception)
    {
        Exception = exception;
    }
    public RpcResult(T? result, Exception? exception)
    {
        Value = result;
        Exception = exception;
    }
    public bool HasException => Exception != null;
}

public interface IClusterResult<T>
{
    IResult<T>? Result { get; }
    IList<URL> Urls { get; }
    IList<BadUrl> BadUrls { get; }
    Exception? ClusterException { get; }
    bool IsThrow { get; }
}

public record ClusterResult<T> : IClusterResult<T>
{
    public IResult<T>? Result { get; private set; }

    public IList<URL> Urls { get; private set; }

    public IList<BadUrl> BadUrls { get; private set; }

    public Exception? ClusterException { get; private set; }

    public bool IsThrow { get; private set; }

    public ClusterResult(IResult<T>? result,
        IList<URL> urls,
        IList<BadUrl> badUrls,
        Exception? clusterException, bool isThrow)
    {
        Result = result;
        Urls = urls;
        BadUrls = badUrls;
        ClusterException = clusterException;
        IsThrow = isThrow;
    }
}

public interface IBaseReturnResult
{
    T? Translate<T>();
}

public record ResponseDataResult 
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public string Trace { get; set; } = string.Empty;
}

public record ResponseDataResult<T> : IBaseReturnResult
    where T : class
{
    public T? Data { get; set; }

    public T1? Translate<T1>()
    {
        var result = (T1?)Data.ChangeType(typeof(T1));
        return result;
    }
}

/// <summary>
/// 路径过滤器
/// </summary>
public class ResponseRpcInterceptor(ILogger<ResponseRpcInterceptor> _logger) : IInterceptor
{
    public async Task<string> UrlCall(string url, ProxyMethodResolverContext context)
    {
        await Task.CompletedTask;
        return url;
    }

    public async Task BeforeCall(IInvocation invocation, RpcContext context)
    {
        await Task.CompletedTask;
    }

    public async Task AfterCall<T>(IInvocation invocation, RpcContext context, IResult<T>? obj)
    {
        if (obj != null && !obj.HasException && obj.Value is ResponseDataResult baseObj)
        {
            if (baseObj.Code != 0)
            {
                throw new ResponseRpcException(baseObj.Msg);
            }
        }
        await Task.CompletedTask;
    }

    public int Order => -999;
}

public class ResponseRpcException : Exception
{
    public ResponseRpcException(string message) : base(message)
    {
    }
}