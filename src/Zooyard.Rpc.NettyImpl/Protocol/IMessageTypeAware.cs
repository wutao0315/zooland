namespace Zooyard.Rpc.NettyImpl.Protocol
{
    /// <summary>
	/// 
	/// </summary>
	public interface IMessageTypeAware
    {

        /// <summary>
        /// return the message type
        /// @return
        /// </summary>
        short TypeCode { get; }
    }
}
