using System;
using System.Text;


namespace Zooyard.Rpc.NettyImpl.Protocol
{


    /// <summary>
    /// The type Merge result message.
    /// 
    /// </summary>
    [Serializable]
	public class MergeResultMessage : AbstractMessage, IMergeMessage
	{
        
		/// <summary>
		/// Get msgs abstract result message [ ].
		/// </summary>
		/// <returns> the abstract result message [ ] </returns>
		public virtual AbstractResultMessage[] Msgs { get; set; }

		public override short TypeCode => MessageType.TYPE_SEATA_MERGE_RESULT;

		public override string ToString()
		{
			var sb = new StringBuilder("MergeResultMessage ");
			if (Msgs == null)
			{
				return sb.ToString();
			}
			foreach (AbstractMessage msg in Msgs)
			{
				sb.Append(msg.ToString()).Append('\n');
			}
			return sb.ToString();
		}
	}

}