namespace Zooyard.Rpc.Support;

public abstract class AbstractClient(URL url) : IClient
{
    public const string RPC_TIMEOUT_KEY = "rpc_timeout";
    public const int DEFAULT_RPC_TIMEOUT = 5000;

    public const string CHECK_TIMEOUT_KEY = "check_timeout";
    public const int DEFAULT_CHECK_TIMEOUT = 1000;

    public virtual string Version => Url.GetParameter(URL.VERSION_KEY, "1.0.0");
    public DateTime ActiveTime { get; set; } = DateTime.Now;
    public abstract string System { get; }
    public int ClientTimeout { get; } = url.GetParameter(RPC_TIMEOUT_KEY, DEFAULT_RPC_TIMEOUT);
    public int CheckTimeout { get; } = url.GetParameter(CHECK_TIMEOUT_KEY, DEFAULT_CHECK_TIMEOUT);
    public URL Url { get; } = url;
    public abstract Task<IInvoker> Refer(CancellationToken cancellationToken = default);
    public abstract Task Open(CancellationToken cancellationToken = default);
    public abstract Task Close(CancellationToken cancellationToken = default);
    public abstract ValueTask DisposeAsync();
    public virtual void Reset() { }
}
