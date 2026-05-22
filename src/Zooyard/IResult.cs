using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.ComponentModel;
using Zooyard.DynamicProxy;
using Zooyard.Rpc;
using Zooyard.Utils;

namespace Zooyard;

public interface IResult
{
    long ElapsedMilliseconds { get; set; }
    bool HasException { get; }
    Exception? Exception { get; }

    object? OriginalValue { get; }
}


public interface IResult<T> : IResult
{
    T? Value { get; }
}

public record RpcResult<T> : IResult<T>
{
    public object? OriginalValue { get; init; }
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

public interface IResultTranslate
{
    T? Translate<T>(object obj);
}

public class ResultTranslate : IResultTranslate
{
    public T? Translate<T>(object obj)
    {
        if (obj is ResponseDataResult<T> responseObj)
        {
            return responseObj.Data;
        }

        if (obj is ResponseMessage rm)
        {
            TypeRegistry registry = CreateTypeRegistryFromType<T>();

            IMessage message = rm.Data.Unpack(registry);

            if (message is T t) 
            {
                return t;
            }

            throw new ArgumentException($"{message.GetType().Name} 不是有效的 {typeof(T).Name}类型");
        }
        return default;

        TypeRegistry CreateTypeRegistryFromType<T>()
        {
            var messageType = typeof(T);

            var descriptorProp = messageType.GetProperty("Descriptor");
           
            if (descriptorProp == null)
                throw new ArgumentException($"{messageType.Name} 不是有效的 Protobuf 消息类型");

            MessageDescriptor descriptor = (MessageDescriptor)descriptorProp.GetValue(null)!;

            return TypeRegistry.FromFiles(descriptor.File);
        }
    }
    
}

public record ResponseDataResult
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public string? Trace { get; set; }
}

public record ResponseDataResult<T> : ResponseDataResult
{
    public T? Data { get; set; }
}

/// <summary>
/// 路径过滤器
/// </summary>
public class ResponseRpcInterceptor : IInterceptor
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
        if (obj != null && !obj.HasException && obj.Value != null && obj.Value is ResponseDataResult baseObj && baseObj.Code != 0)
        {
            throw new ResponseRpcException(baseObj.Msg, baseObj.Trace);
        }

        if (obj != null && !obj.HasException && obj.Value != null && obj.Value is ResponseMessage rm && rm.Code != 0)
        {
            throw new ResponseRpcException(rm.Msg, rm.Trace);
        }
        await Task.CompletedTask;
    }

    public int Order => -999;
}

public class ResponseRpcException(string message, string? trace = null) 
    : Exception(message, string.IsNullOrWhiteSpace(trace)? null: new Exception(trace) )
{
}