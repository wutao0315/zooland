using System.Text;
using Zooyard.Rpc.NettyImpl.Protocol;


namespace Zooyard.Rpc.NettyImpl.Support;

/// <summary>
/// The type Netty pool key.
/// 
/// </summary>
public class NettyPoolKey
{

	private string address;

	/// <summary>
	/// Instantiates a new Netty pool key.
	/// </summary>
	/// <param name="transactionRole"> the client role </param>
	/// <param name="address">         the address </param>
	public NettyPoolKey(string address)
	{
		this.address = address;
	}

	/// <summary>
	/// Instantiates a new Netty pool key.
	/// </summary>
	/// <param name="address">         the address </param>
	/// <param name="message">         the message </param>
	public NettyPoolKey(string address, AbstractMessage message)
	{
		this.address = address;
		this.Message = message;
	}


	/// <summary>
	/// Gets get address.
	/// </summary>
	/// <returns> the get address </returns>
	public virtual string Address => address;

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
		sb.Append("address:");
		sb.Append(address);
		sb.Append(',');
		sb.Append("msg:< ");
		sb.Append(Message.ToString());
		sb.Append(" >");
		return sb.ToString();
	}
}
