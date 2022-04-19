namespace Zooyard.Rpc.NettyImpl.Protocol;

/// <summary>
/// The type Rpc message.
/// 
/// </summary>
public class RpcMessage
	{
		/// <summary>
		/// Gets id.
		/// </summary>
		/// <returns> the id </returns>
		public virtual int Id { get; set; }
    /// <summary>
    /// message type
    /// </summary>
    public virtual byte MessageType { get; set; }
    /// <summary>
    /// codec
    /// </summary>
    public virtual byte Codec { get; set; }
    /// <summary>
    /// compressor
    /// </summary>
    public virtual byte Compressor { get; set; }
    /// <summary>
    /// Gets head map.
    /// </summary>
    public virtual IDictionary<string, string> HeadMap { get; set; }
    public virtual object Body { get; set; }
}
