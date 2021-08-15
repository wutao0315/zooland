
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Serializer
{

	/// <summary>
	/// The interface Codec.
	/// 
	/// </summary>
	public interface ISerializer
	{

		/// <summary>
		/// Encode object to byte[].
		/// </summary>
		/// @param <T> the type parameter </param>
		/// <param name="t">   the t </param>
		/// <returns> the byte [ ] </returns>
		byte[] Serialize<T>(T t) where T : AbstractMessage;

		/// <summary>
		/// Decode t from byte[].
		/// </summary>
		/// @param <T>   the type parameter </param>
		/// <param name="bytes"> the bytes </param>
		/// <returns> the t </returns>
		T Deserialize<T>(byte[] bytes) where T : AbstractMessage;
	}

}