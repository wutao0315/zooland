using System;


namespace Zooyard.Rpc.NettyImpl.Protocol
{


    /// <summary>
    /// The type Abstract result message.
    /// 
    /// </summary>
    [Serializable]
	public abstract class AbstractResultMessage : AbstractMessage, IMergedMessage
	{
		public override abstract short TypeCode {get;}

		/// <summary>
		/// Gets result code.
		/// </summary>
		/// <returns> the result code </returns>
		public virtual ResultCode? ResultCode { get; set; }

		/// <summary>
		/// Gets msg.
		/// </summary>
		/// <returns> the msg </returns>
		public virtual string Msg { get; set; }

	}

}