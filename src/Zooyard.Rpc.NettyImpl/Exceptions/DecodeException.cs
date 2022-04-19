namespace Zooyard.Rpc.NettyImpl.Exceptions;

/// <summary>
/// </summary>
public class DecodeException : Exception
{

	public DecodeException(Exception throwable) : base(throwable.Message, throwable)
	{
	}
}
