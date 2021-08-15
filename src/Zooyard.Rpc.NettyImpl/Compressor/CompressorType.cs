using System.Collections.Generic;

namespace Zooyard.Rpc.NettyImpl.Compressor
{
	public enum CompressorType 
	{
		NONE = 0,
		GZIP = 1,
		ZIP = 2,
		SEVENZ = 3,
		BZIP2 = 4
	}
}