namespace Zooyard.Rpc.Support;

public abstract class AbstractClient: IClient
{
    public virtual string Version { get { return Url.GetParameter(URL.VERSION_KEY)!; } }
    public DateTime ActiveTime { get; set; } = DateTime.Now;
    public abstract int ClientTimeout { get; }
    public abstract URL Url { get; }
    public abstract Task<IInvoker> Refer(CancellationToken cancellationToken = default);
    public abstract Task Open(CancellationToken cancellationToken = default);
    public abstract Task Close(CancellationToken cancellationToken = default);
    public abstract ValueTask DisposeAsync();
    public virtual void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
    public virtual void Reset() { }
}
