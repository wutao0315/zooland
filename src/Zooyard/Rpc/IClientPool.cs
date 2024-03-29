﻿namespace Zooyard.Rpc;

public interface IClientPool: IDisposable,IAsyncDisposable
{
    string ServiceName { get; set; }
    //string Version { get; set; }
    //URL Address { get; set; }
    //URL Url { get; set; }
    Type? ProxyType { get; set; }

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
