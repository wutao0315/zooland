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
}
public record ResponseDataResult<T> : ResponseDataResult, IBaseReturnResult
    where T : class
{
    public T? Data { get; set; }

    public T1? Translate<T1>()
    {
        var result = (T1?)Data.ChangeType(typeof(T1));
        return result;
    }
}