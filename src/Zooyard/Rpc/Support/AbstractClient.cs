namespace Zooyard.Rpc.Support;

public abstract class AbstractClient(int clientTimeout, URL url) : IClient
{
    public virtual string Version => Url.GetParameter(URL.VERSION_KEY, "1.0.0");
    public DateTime ActiveTime { get; set; } = DateTime.Now;
    public abstract string System { get; }
    public int ClientTimeout { get; } = clientTimeout;
    public URL Url { get; } = url;
    public abstract Task<IInvoker> Refer(CancellationToken cancellationToken = default);
    public abstract Task Open(CancellationToken cancellationToken = default);
    public abstract Task Close(CancellationToken cancellationToken = default);
    public abstract ValueTask DisposeAsync();
    public virtual void Reset() { }
}
