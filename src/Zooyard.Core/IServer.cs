using System;

namespace Zooyard.Core
{
    public interface IServer : IDisposable
    {
        void Export();
    }
}
