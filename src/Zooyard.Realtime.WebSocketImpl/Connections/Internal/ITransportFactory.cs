namespace Zooyard.WebSocketsImpl.Connections.Internal;

public interface ITransportFactory
{
    ITransport CreateTransport(bool useStatefulReconnect);
}