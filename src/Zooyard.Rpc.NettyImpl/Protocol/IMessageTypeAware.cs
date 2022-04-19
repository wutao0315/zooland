namespace Zooyard.Rpc.NettyImpl.Protocol;

/// <summary>
/// 
/// </summary>
public interface IMessageTypeAware
{

    /// <summary>
    /// return the message type
    /// </summary>
    short TypeCode { get; }
}
