using System.IO.Pipelines;
using Zooyard.Realtime;

namespace Zooyard.WebSocketsImpl.Connections.Internal;

public interface ITransport : IDuplexPipe
{
    string Name { get; }
    Task StartAsync(Uri url, TransferFormat transferFormat, CancellationToken cancellationToken = default);
    Task StopAsync();
}
