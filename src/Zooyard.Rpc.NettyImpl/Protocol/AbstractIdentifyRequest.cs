using System;


namespace Zooyard.Rpc.NettyImpl.Protocol
{
    /// <summary>
    /// The type Abstract identify request.
    /// 
    /// </summary>
    [Serializable]
	public abstract class AbstractIdentifyRequest : AbstractMessage
	{

        /// <summary>
		/// Instantiates a new Abstract identify request.
		/// </summary>
		/// <param name="applicationId">           the application id </param>
		/// <param name="transactionServiceGroup"> the transaction service group </param>
		public AbstractIdentifyRequest(string applicationId,string transactionServiceGroup) 
            : this(applicationId,transactionServiceGroup, null)
        {
        }
        /// <summary>
		/// Instantiates a new Abstract identify request.
		/// </summary>
		/// <param name="applicationId">           the application id </param>
		/// <param name="transactionServiceGroup"> the transaction service group </param>
		/// <param name="extraData">               the extra data </param>
		public AbstractIdentifyRequest(
            string applicationId, 
            string transactionServiceGroup,
            string extraData)
        {
            this.ApplicationId = applicationId;
            this.TransactionServiceGroup = transactionServiceGroup;
            this.ExtraData = extraData;
        }

		/// <summary>
		/// Gets version.
		/// </summary>
		/// <returns> the version </returns>
		public virtual string Version { get; set; } = Protocol.Version.Current;

        /// <summary>
        /// Gets application id.
        /// </summary>
        /// <returns> the application id </returns>
        public virtual string ApplicationId { get; set; }


		/// <summary>
		/// Gets transaction service group.
		/// </summary>
		/// <returns> the transaction service group </returns>
		public virtual string TransactionServiceGroup { get; set; }


		/// <summary>
		/// Gets extra data.
		/// </summary>
		/// <returns> the extra data </returns>
		public virtual string ExtraData { get; set; }

	}
}