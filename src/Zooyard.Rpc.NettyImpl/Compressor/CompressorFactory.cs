using System.Collections.Concurrent;
//using Zooyard.Loader;


namespace Zooyard.Rpc.NettyImpl.Compressor;

/// <summary>
/// the type compressor factory
/// </summary>
public class CompressorFactory
{

	/// <summary>
	/// The constant COMPRESSOR_MAP.
	/// </summary>
	protected internal static readonly ConcurrentDictionary<CompressorType, ICompressor> COMPRESSOR_MAP = new ();

	static CompressorFactory()
	{
		COMPRESSOR_MAP[CompressorType.NONE] = new NoneCompressor();
	}

	/// <summary>
	/// Get compressor by code.
	/// </summary>
	/// <param name="code"> the code </param>
	/// <returns> the compressor </returns>
	public static ICompressor GetCompressor(byte code)
	{
		var type = (CompressorType)code;

		//ICompressor impl = COMPRESSOR_MAP.GetOrAdd(type, (key)=>EnhancedServiceLoader.Load<ICompressor>(type.ToString()));
		ICompressor impl = new NoneCompressor();
		return impl;
	}

	/// <summary>
	/// None compressor
	/// </summary>
	//[LoadLevel(name: "NONE")]
	public class NoneCompressor : ICompressor
	{
		public virtual byte[] Compress(byte[] bytes)
		{
			return bytes;
		}

		public virtual byte[] Decompress(byte[] bytes)
		{
			return bytes;
		}
	}

}

