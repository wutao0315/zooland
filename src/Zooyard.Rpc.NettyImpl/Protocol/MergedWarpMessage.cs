using System.Text;


namespace Zooyard.Rpc.NettyImpl.Protocol;

/// <summary>
/// The type Merged warp message.
/// 
/// </summary>
[Serializable]
public class MergedWarpMessage : AbstractMessage, IMergeMessage
{
       
        /// <summary>
        /// The Msgs.
        /// </summary>
        public IList<AbstractMessage> msgs = new List<AbstractMessage>();
	/// <summary>
	/// The Msg ids.
	/// </summary>
	public IList<int> msgIds = new List<int>();

	public override short TypeCode => MessageType.TYPE_SEATA_MERGE;

	public override string ToString()
	{
		var sb = new StringBuilder("SeataMergeMessage ");
		foreach (AbstractMessage msg in msgs)
		{
			sb.Append(msg.ToString()).Append("\n");
		}
		return sb.ToString();
	}
}

