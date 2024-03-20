using System.Text;
using System.Text.Json;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;
using Zooyard.DotNettyImpl.Transport.Codec;

namespace Zooyard.DotNettyImpl.Codec;

public sealed class JsonTransportMessageEncoder : ITransportMessageEncoder
{
    public byte[] Encode(TransportMessage message)
    {
        //var result = MemoryPackSerializer.Serialize(message);
        //return result;
        //if (message == null)
        //    return Array.Empty<byte>();

        //using var ms = new MemoryStream();
        //message.WriteTo(ms);
        //return ms.ToArray();

        //var content = JsonConvert.SerializeObject(message);
        //return Encoding.UTF8.GetBytes(content);

        var result = JsonSerializer.SerializeToUtf8Bytes(message, JsonTransportMessageCodecFactory._option);
        return result;
    }
}
