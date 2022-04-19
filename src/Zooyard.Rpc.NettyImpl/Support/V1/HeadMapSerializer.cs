using DotNetty.Buffers;
using Zooyard.Rpc.NettyImpl.Constant;

namespace Zooyard.Rpc.NettyImpl.Support.V1;

/// <summary>
/// Common serializer of map (this generally refers to header).
/// </summary>
public class HeadMapSerializer
{
    private static readonly HeadMapSerializer INSTANCE = new();

    private HeadMapSerializer(){}

    public static HeadMapSerializer getInstance()
    {
        return INSTANCE;
    }

    /// <summary>
    /// encode head map
    /// </summary>
    /// <param name="map">map header map</param>
    /// <param name="output">out ByteBuf</param>
    /// <returns> length of head map bytes</returns>
    public int Encode(IDictionary<string, string> map, IByteBuffer output)
    {
        if ((map?.Count??0)<=0 || output == null) {
            return 0;
        }
        int start = output.WriterIndex;
        foreach (var entry in map)
        {
            var key = entry.Key;
            var value = entry.Value;
            if (key != null)
            {
                WriteString(output, key);
                WriteString(output, value);
            }
        }

        return output.WriterIndex - start;
    }

    /**
     * decode head map
     *
     * @param in ByteBuf
     * @param length of head map bytes
     * @return header map
     */
    public IDictionary<string, string> Decode(IByteBuffer input, int length)
    {
        var map = new Dictionary<string, string>();
        if (input == null || input.ReadableBytes == 0 || length == 0) {
            return map;
        }
        int tick = input.ReaderIndex;
        while (input.ReaderIndex - tick < length) {
            var key = ReadString(input);
            var value = ReadString(input);
            map.Add(key, value);
        }

        return map;
    }

    /**
     * Write string
     *
     * @param out ByteBuf
     * @param str String
     */
    protected void WriteString(IByteBuffer output, string str)
    {
        if (str == null)
        {
            output.WriteShort(-1);
        }
        else if (string.IsNullOrEmpty(str))
        {
            output.WriteShort(0);
        }
        else
        {
            byte[] bs = Constants.DEFAULT_CHARSET.GetBytes(str);
            output.WriteShort(bs.Length);
            output.WriteBytes(bs);
        }
    }
    /**
     * Read string
     *
     * @param in ByteBuf
     * @return String
     */
    protected string ReadString(IByteBuffer input)
    {
        int length = input.ReadShort();
        if (length < 0)
        {
            return null;
        }
        else if (length == 0)
        {
            return string.Empty;
        }
        else
        {
            byte[] value = new byte[length];
            input.ReadBytes(value);

            var result = Constants.DEFAULT_CHARSET.GetString(value);
            return result;
        }
    }
}
