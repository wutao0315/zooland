using System;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IServer : IDisposable
    {
        Task Export();
    }
}
