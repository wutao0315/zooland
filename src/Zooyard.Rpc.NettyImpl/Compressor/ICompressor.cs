
namespace Zooyard.Rpc.NettyImpl.Compressor;

/// <summary>
/// 压缩接口设计
/// </summary>
public interface ICompressor
{

	/// <summary>
	/// compress byte[] to byte[]. </summary>
	/// <param name="bytes"> the bytes </param>
	/// <returns> the byte[] </returns>
	byte[] Compress(byte[] bytes);

	/// <summary>
	/// decompress byte[] to byte[]. </summary>
	/// <param name="bytes"> the bytes </param>
	/// <returns> the byte[] </returns>
	byte[] Decompress(byte[] bytes);
}
