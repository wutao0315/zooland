namespace Zooyard.Rpc;

public interface IClientPool: IAsyncDisposable
{
    string Name { get; internal set; }
    string ServiceName { get; internal set; }
    Type? ProxyType { get; internal set; }

    Task<IClient> GetClient(URL url);

    /// <summary>
    /// 归还一个连接至连接池
    /// </summary>
    /// <param name="client">连接</param>
    Task Recovery(IClient client);
    /// <summary>
    /// 销毁连接
    /// </summary>
    /// <param name="client">连接</param>
    Task DestoryClient(IClient client);
    /// <summary>
    /// 超时清除
    /// </summary>
    /// <param name="overTime"></param>
    Task TimeOver(DateTime overTime);
}
