using System.Text;
using Zooyard.Rpc.DotNettyImpl.Messages;

namespace Zooyard.Rpc.DotNettyImpl.Codec;

public sealed class JsonTransportMessageDecoder : ITransportMessageDecoder
{

    public TransportMessage Decode(byte[] data)
    {
        var content = Encoding.UTF8.GetString(data);
        var message = content.DeserializeJson<TransportMessage>();
        if (message.IsInvokeMessage() && message.Content is string msgContent)
        {
            message.Content = msgContent.DeserializeJson<RemoteInvokeMessage>();
        }
        if (message.IsInvokeResultMessage() && message.Content is string msgContentResult)
        {
            message.Content = msgContentResult.DeserializeJson<RemoteInvokeResultMessage>();
        }
        return message;
    }

}
