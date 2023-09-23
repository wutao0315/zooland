namespace Zooyard.Diagnositcs;


public record EventDataStore
{
    public EventDataStore(string system, string clusterName, URL url, IInvocation invocation) 
    {
        System = system;
        ClusterName = clusterName;
        Url = url;
        Invocation = invocation;
    }
    public long ActiveTimestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string System { get; init; }
    public string ClusterName { get; init; }
    public URL Url { get; init; }
    public IInvocation Invocation { get; init; }
    public object? Result { get; set; }
    public long? Elapsed { get; set; }
    public Exception? Exception { get; set; }
}
