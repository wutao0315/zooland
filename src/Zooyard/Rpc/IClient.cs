namespace Zooyard.Rpc;

public interface IClient : IAsyncDisposable
{
    string System { get; }
    URL Url { get; }
    Task<IInvoker> Refer(CancellationToken cancellationToken = default);
    string Version { get; }
    int ClientTimeout{get;}
    DateTime ActiveTime { get; set; }
    Task Open(CancellationToken cancellationToken = default);
    Task Close(CancellationToken cancellationToken = default);
    void Reset();

}
