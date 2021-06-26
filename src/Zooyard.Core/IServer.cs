using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IServer : IAsyncDisposable
    {
        Task Export(CancellationToken cancellationToken);
    }
}
