namespace Zooyard;

public interface IZooyardPools:IDisposable
{
    /// <summary>
    /// clear all cache
    /// </summary>
    void CacheClear();
    /// <summary>
    /// 执行远程调用
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>

    Task<IResult<T>?> Invoke<T>(IInvocation invocation);
}
