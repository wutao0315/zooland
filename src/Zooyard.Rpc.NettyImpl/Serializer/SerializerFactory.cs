using System.Collections.Concurrent;
using Zooyard.Loader;
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl.Serializer;

/// <summary>
/// The type Codec factory.
/// 
/// </summary>
public class SerializerFactory
{

	/// <summary>
	/// The constant CODEC_MAP.
	/// </summary>
	protected internal static readonly ConcurrentDictionary<SerializerType, ISerializer> CODEC_MAP = new ();

	/// <summary>
	/// Get serializeCode serializeCode.
	/// </summary>
	/// <param name="serializeCode"> the code </param>
	/// <returns> the serializeCode </returns>
	public static ISerializer GetSerializer(byte serializeCode)
	{
		SerializerType serializerType = (SerializerType)serializeCode;
		ISerializer codecImpl = CODEC_MAP.GetOrAdd(serializerType,(key)=> EnhancedServiceLoader.Load<ISerializer>(serializerType.ToString()));
		return codecImpl;
	}

	/// <summary>
	/// Encode byte [ ].
	/// </summary>
	/// @param <T>   the type parameter </param>
	/// <param name="serializeCode"> the serializeCode </param>
	/// <param name="t">     the t </param>
	/// <returns> the byte [ ] </returns>
	public static byte[] Encode<T>(byte serializeCode, T t) where T: AbstractMessage
	{
		return GetSerializer(serializeCode).Serialize<T>(t);
	}

	/// <summary>
	/// Decode t.
	/// </summary>
	/// @param <T>   the type parameter </param>
	/// <param name="codec"> the code </param>
	/// <param name="bytes"> the bytes </param>
	/// <returns> the t </returns>
	public static T Decode<T>(byte codec, byte[] bytes) where T : AbstractMessage
	{
		return GetSerializer(codec).Deserialize<T>(bytes);
	}
}
