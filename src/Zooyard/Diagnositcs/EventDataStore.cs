using System;

namespace Zooyard.Diagnositcs;


public sealed record EventDataStore(string system, string clusterName, URL url, IInvocation invocation)
{
    public long ActiveTimestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string System { get; init; } = system;
    public string ClusterName { get; init; } = clusterName;
    public URL Url { get; init; } = url;
    public IInvocation Invocation { get; init; } = invocation;
    public object? Result { get; set; }
    public long? Elapsed { get; set; }
    public Exception? Exception { get; set; }
}
