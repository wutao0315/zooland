using System;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IServer : IAsyncDisposable
    {
        Task Export();
    }
}
