using Newtonsoft.Json;
using System.Text;
using Zooyard.DotNettyImpl.Messages;
using Zooyard.DotNettyImpl.Transport;

namespace Zooyard.DotNettyImpl.Codec;

public sealed class JsonTransportMessageDecoder : ITransportMessageDecoder
{

    public TransportMessage Decode(byte[] data)
    {
        //var result = MemoryPackSerializer.Deserialize<TransportMessage>(data)!;
        //return result;

        //using var ms = new MemoryStream();
        //ms.Write(data, 0, data.Length);
        //ms.Seek(0, SeekOrigin.Begin);

        //var parser = new MessageParser<TransportMessage>(() => new TransportMessage());
        //return parser.ParseFrom(ms);

        var content = Encoding.UTF8.GetString(data);
        var message = JsonConvert.DeserializeObject<TransportMessage>(content)!;
        if (message.IsInvokeMessage())
        {
            message.Content = JsonConvert.DeserializeObject<RemoteInvokeMessage>(message.Content.ToString());
        }
        if (message.IsInvokeResultMessage())
        {
            message.Content = JsonConvert.DeserializeObject<RemoteInvokeResultMessage>(message.Content.ToString());
        }
        return message;

        //var message = JsonSerializer.Deserialize<TransportMessage>(data, JsonTransportMessageCodecFactory._option)!;
        //if (message.IsInvokeMessage())
        //{
        //    if (message.Content is string msgContent)
        //    {
        //        message.Content = JsonSerializer.Deserialize<RemoteInvokeMessage>(msgContent, JsonTransportMessageCodecFactory._option);
        //    }
        //    else
        //    {
        //        message.Content = JsonSerializer.Deserialize<RemoteInvokeMessage>(message.Content?.ToString() ?? "{}", JsonTransportMessageCodecFactory._option);
        //    }
        //}
        //if (message.IsInvokeResultMessage())
        //{
        //    if (message.Content is string msgContent)
        //    {
        //        message.Content = JsonSerializer.Deserialize<RemoteInvokeResultMessage>(msgContent, JsonTransportMessageCodecFactory._option);
        //    }
        //    else
        //    {
        //        message.Content = JsonSerializer.Deserialize<RemoteInvokeResultMessage>(message.Content?.ToString() ?? "{}", JsonTransportMessageCodecFactory._option);
        //    }
        //}
        //return message;
    }

}
