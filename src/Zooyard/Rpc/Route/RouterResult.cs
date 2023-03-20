namespace Zooyard.Rpc.Route;

public record RouterResult<T>
{

    public RouterResult(List<T>? result)
    {
        IsNeedContinueRoute = true;
        Result = result;
        Message = null;
    }

    public RouterResult(List<T> result, string message)
    {
        IsNeedContinueRoute = true;
        Result = result;
        Message = message;
    }

    public RouterResult(bool needContinueRoute, List<T> result, string message)
    {
        IsNeedContinueRoute = needContinueRoute;
        Result = result;
        Message = message;
    }

    public bool IsNeedContinueRoute { get; init; }

    public List<T>? Result { get; init; }
    public string? Message { get; init; }
}
