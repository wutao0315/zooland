namespace Zooyard.Diagnositcs;


public record EventDataStore
{
    public long? OperationTimestamp { get; set; }

    public string Operation { get; set; } = default!;

    public long? ElapsedTimeMs { get; set; }

    public object? TransportMessage { get; set; }

    public Exception? Exception { get; set; }
}
