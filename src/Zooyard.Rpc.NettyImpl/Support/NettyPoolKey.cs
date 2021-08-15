using System.Collections.Generic;
using System.Text;
using Zooyard.Rpc.NettyImpl.Protocol;


namespace Zooyard.Rpc.NettyImpl.Support
{
	/// <summary>
	/// The type Netty pool key.
	/// 
	/// </summary>
	public class NettyPoolKey
	{

		private TransactionRole transactionRole;
		private string address;

		/// <summary>
		/// Instantiates a new Netty pool key.
		/// </summary>
		/// <param name="transactionRole"> the client role </param>
		/// <param name="address">         the address </param>
		public NettyPoolKey(TransactionRole transactionRole, string address)
		{
			this.transactionRole = transactionRole;
			this.address = address;
		}

		/// <summary>
		/// Instantiates a new Netty pool key.
		/// </summary>
		/// <param name="transactionRole"> the client role </param>
		/// <param name="address">         the address </param>
		/// <param name="message">         the message </param>
		public NettyPoolKey(TransactionRole transactionRole, string address, AbstractMessage message)
		{
			this.transactionRole = transactionRole;
			this.address = address;
			this.Message = message;
		}

		/// <summary>
		/// Gets get client role.
		/// </summary>
		/// <returns> the get client role </returns>
		public virtual TransactionRole GetTransactionRole()
		{
			return transactionRole;
		}

		/// <summary>
		/// Sets set client role.
		/// </summary>
		/// <param name="transactionRole"> the client role </param>
		/// <returns> the client role </returns>
		public virtual NettyPoolKey SetTransactionRole(TransactionRole transactionRole)
		{
			this.transactionRole = transactionRole;
			return this;
		}

		/// <summary>
		/// Gets get address.
		/// </summary>
		/// <returns> the get address </returns>
		public virtual string Address
		{
			get
			{
				return address;
			}
		}

		/// <summary>
		/// Sets set address.
		/// </summary>
		/// <param name="address"> the address </param>
		/// <returns> the address </returns>
		public virtual NettyPoolKey SetAddress(string address)
		{
			this.address = address;
			return this;
		}

		/// <summary>
		/// Gets message.
		/// </summary>
		/// <returns> the message </returns>
		public virtual AbstractMessage Message { get; set; }


		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append("transactionRole:");
			sb.Append(transactionRole);
			sb.Append(',');
			sb.Append("address:");
			sb.Append(address);
			sb.Append(',');
			sb.Append("msg:< ");
			sb.Append(Message.ToString());
			sb.Append(" >");
			return sb.ToString();
		}

        public enum TransactionRole 
        {
            TMROLE=1,
            RMROLE=2,
            SERVERROLE=3
        }
	}
}