namespace Zooyard.Rpc.NettyImpl.Protocol;

/// <summary>
/// The type Heartbeat message.
/// 
/// </summary>
[Serializable]
	public class HeartbeatMessage
	{
    /// <summary>
    /// The constant PING.
    /// </summary>
    public static readonly HeartbeatMessage PING = new (true);
    /// <summary>
    /// The constant PONG.
    /// </summary>
    public static readonly HeartbeatMessage PONG = new (false);

    private HeartbeatMessage(bool ping)
    {
        this.Ping = ping;
    }

    public virtual bool Ping { get; set; } = true;

    public override string ToString()
    {
        return this.Ping ? "services ping" : "services pong";
    }
}
