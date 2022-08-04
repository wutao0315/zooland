namespace Zooyard;

public interface IResult<T>
{
    T? Value { get; }
    long ElapsedMilliseconds{get;}
    bool HasException { get; }
    Exception? Exception { get; }
}

public class RpcResult<T> : IResult<T>
{
    public T? Value { get; private set; }
    public long ElapsedMilliseconds { get; private set; }
    public Exception? Exception { get; private set; }

    public RpcResult(long elapsedMilliseconds)
    {
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public RpcResult(T result, long elapsedMilliseconds)
    {
        Value = result;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public RpcResult(Exception? exception)
    {
        Exception = exception;
    }

    public bool HasException => Exception != null;
}

public interface IClusterResult<T>
{
    IResult<T> Result { get; }
    IList<URL> Urls { get; }
    IList<BadUrl> BadUrls { get; }
    Exception? ClusterException { get; }
    bool IsThrow { get; }
}

public class ClusterResult<T> : IClusterResult<T>
{
    public IResult<T> Result { get; private set; }

    public IList<URL> Urls { get; private set; }

    public IList<BadUrl> BadUrls { get; private set; }

    public Exception? ClusterException { get; private set; }

    public bool IsThrow { get; private set; }

    public ClusterResult(IResult<T> result, IList<URL> urls, IList<BadUrl> badUrls, Exception? clusterException, bool isThrow)
    {
        Result = result;
        Urls = urls;
        BadUrls = badUrls;
        ClusterException = clusterException;
        IsThrow = isThrow;
    }
}
