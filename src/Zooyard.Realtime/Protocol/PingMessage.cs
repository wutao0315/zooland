namespace Zooyard.Realtime.Protocol;

public record PingMessage : RpcMessage
{
    public static readonly PingMessage Instance = new();

    private PingMessage()
    {
    }
}
