namespace Zooyard;

public interface IServer : IAsyncDisposable
{
    Task Export(CancellationToken cancellationToken);
}
